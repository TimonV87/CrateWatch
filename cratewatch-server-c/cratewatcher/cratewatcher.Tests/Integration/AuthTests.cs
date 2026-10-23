using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CrateWatch.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFixture api)
{
    [Fact]
    public async Task Login_with_the_right_password_returns_a_week_long_token()
    {
        var (client, _, username) = await api.NewUserAsync();

        var response = await api.LoginAsync(client, username, "correct-password");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("access_token").GetString()));
        Assert.Equal(7 * 24 * 60 * 60, body.GetProperty("expires_in").GetInt32());
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_401()
    {
        var (client, _, username) = await api.NewUserAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.LoginAsync(client, username, "wrong-password")).StatusCode);
    }

    [Fact]
    public async Task Login_with_an_unknown_user_is_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.LoginAsync(api.CreateClient(), "nobody-at-all", "whatever-pass")).StatusCode);
    }

    [Fact]
    public async Task The_sixth_login_from_one_client_ip_is_429_but_another_ip_is_not_affected()
    {
        var client = api.CreateClient();
        var ip = api.NextClientIp();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            statuses.Add((await api.LoginAsync(client, "nobody", "wrong-password", ip)).StatusCode);
        }

        Assert.Equal([.. Enumerable.Repeat(HttpStatusCode.Unauthorized, 5), HttpStatusCode.TooManyRequests], statuses);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.LoginAsync(client, "nobody", "wrong-password")).StatusCode);
    }

    [Fact]
    public async Task Faking_extra_forwarded_ips_does_not_reset_the_login_limit()
    {
        var client = api.CreateClient();
        var ip = api.NextClientIp();
        for (var i = 0; i < 5; i++)
        {
            await api.LoginAsync(client, "nobody", "wrong-password", ip);
        }

        // An attacker prepends their own made-up address; the app still counts the real client (second from the right).
        var response = await api.LoginAsync(client, "nobody", "wrong-password", $"203.0.113.9, {ip}");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task There_is_no_register_endpoint()
    {
        var response = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new { username = "new-user", password = "supersecret1" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/artists")]
    [InlineData("/api/v1/artists/search?query=radiohead")]
    [InlineData("/api/v1/releases?state=OWNED")]
    public async Task Api_calls_without_a_token_are_401(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task A_valid_token_stops_working_when_the_user_is_deleted()
    {
        var (client, userId, _) = await api.NewUserAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/artists")).StatusCode);

        await api.DeleteUserAsync(userId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/artists")).StatusCode);
    }
}
