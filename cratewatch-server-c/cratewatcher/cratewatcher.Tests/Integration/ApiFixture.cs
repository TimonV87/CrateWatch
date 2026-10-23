using System.Net.Http.Headers;
using System.Net.Http.Json;
using CrateWatch.MusicBrainz;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CrateWatch.Tests.Integration;

/// <summary>
/// The real app, in-process, on a throwaway Postgres 18 (started with Docker). Data is seeded straight into the
/// database, so no test calls MusicBrainz or any other outside service.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18").Build();
    private NpgsqlDataSource db = null!;
    private WebApplicationFactory<Program> factory = null!;
    private int loginCounter;

    /// <summary>Stands in for musicbrainz.org, so no test needs the internet.</summary>
    public FakeMusicBrainz MusicBrainz { get; } = new();

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        // Program.cs reads these while it builds the host, so they must be set before the factory starts it.
        var connection = new NpgsqlConnectionStringBuilder(postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("DB_HOST", connection.Host);
        Environment.SetEnvironmentVariable("DB_PORT", connection.Port.ToString());
        Environment.SetEnvironmentVariable("DB_NAME", connection.Database);
        Environment.SetEnvironmentVariable("DB_USER", connection.Username);
        Environment.SetEnvironmentVariable("DB_PASSWORD", connection.Password);
        Environment.SetEnvironmentVariable("JWT_GENERATOR_SIGNATURE_SECRET", "integration-test-secret-0123456789abcdef");
        Environment.SetEnvironmentVariable("COVER_ART_STORAGE_PATH", Path.Combine(Path.GetTempPath(), "cw-it-cover"));
        Environment.SetEnvironmentVariable("ARTIST_PHOTO_STORAGE_PATH", Path.Combine(Path.GetTempPath(), "cw-it-photo"));

        db = NpgsqlDataSource.Create(postgres.GetConnectionString());
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddHttpClient<MusicBrainzClient>().ConfigurePrimaryHttpMessageHandler(() => MusicBrainz)));
        factory.CreateClient().Dispose(); // starts the app, which runs the database migration
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await db.DisposeAsync();
        await postgres.DisposeAsync();
    }

    public HttpClient CreateClient() => factory.CreateClient();

    /// <summary>A fresh fake client IP (as NPMplus would send it), so tests never share the login rate limit.</summary>
    public string NextClientIp() => $"10.{Interlocked.Increment(ref loginCounter) / 250}.{loginCounter % 250}.1, 10.0.0.1";

    public async Task<HttpResponseMessage> LoginAsync(HttpClient client, string username, string password, string? forwardedFor = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/login")
        {
            Content = JsonContent.Create(new { username, password }),
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor ?? NextClientIp());
        return await client.SendAsync(request);
    }

    /// <summary>Creates a user straight in the database and returns a client that is logged in as them.</summary>
    public async Task<(HttpClient Client, Guid UserId, string Username)> NewUserAsync()
    {
        var username = $"user-{Guid.NewGuid():N}";
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password", workFactor: 4);
        var userId = await ScalarAsync<Guid>("INSERT INTO users (username, password_hash) VALUES ($1, $2) RETURNING id", username, hash);

        var client = CreateClient();
        var response = await LoginAsync(client, username, "correct-password");
        response.EnsureSuccessStatusCode();
        var token = (await response.Content.ReadFromJsonAsync<LoginBody>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, userId, username);
    }

    public Task<Guid> NewArtistAsync() =>
        ScalarAsync<Guid>("INSERT INTO artists (name, musicbrainz_id) VALUES ($1, $2) RETURNING id", "Test Artist", Guid.NewGuid().ToString());

    public Task<Guid> NewReleaseAsync(Guid artistId) =>
        ScalarAsync<Guid>("INSERT INTO cd_releases (artist_id, title, release_type) VALUES ($1, $2, 'STUDIO_ALBUM') RETURNING id", artistId, $"Album {Guid.NewGuid():N}");

    public async Task TrackAsync(Guid userId, Guid artistId) =>
        await ScalarAsync<Guid>("INSERT INTO user_artists (user_id, artist_id) VALUES ($1, $2) RETURNING id", userId, artistId);

    public async Task DeleteUserAsync(Guid userId)
    {
        await using var command = db.CreateCommand("DELETE FROM users WHERE id = $1");
        command.Parameters.AddWithValue(userId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> ScalarAsync<T>(string sql, params object[] parameters)
    {
        await using var command = db.CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter);
        }
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private sealed record LoginBody([property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
