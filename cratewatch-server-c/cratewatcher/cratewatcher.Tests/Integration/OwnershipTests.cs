using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CrateWatch.Tests.Integration;

[Collection(ApiCollection.Name)]
public class OwnershipTests(ApiFixture api)
{
    private static Task<HttpResponseMessage> SetState(HttpClient client, Guid releaseId, string state) =>
        client.PutAsJsonAsync($"/api/v1/releases/{releaseId}/state", new { state });

    [Fact]
    public async Task The_owner_sees_their_artist_and_releases_with_the_default_studio_view()
    {
        var (owner, ownerId, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        var releaseId = await api.NewReleaseAsync(artistId);
        await api.TrackAsync(ownerId, artistId);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/artists/{artistId}")).StatusCode);
        var releases = await owner.GetFromJsonAsync<JsonElement>($"/api/v1/artists/{artistId}/releases");
        Assert.Equal(releaseId, releases[0].GetProperty("id").GetGuid());
        Assert.Equal("NONE", releases[0].GetProperty("state").GetString());
    }

    [Fact]
    public async Task Another_user_cannot_read_an_artist_they_do_not_track()
    {
        var (_, ownerId, _) = await api.NewUserAsync();
        var (stranger, _, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        await api.TrackAsync(ownerId, artistId);

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/artists/{artistId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/v1/artists/{artistId}/releases")).StatusCode);
        Assert.Equal("[]", await stranger.GetStringAsync("/api/v1/artists"));
    }

    [Fact]
    public async Task Another_user_cannot_change_the_state_of_a_release_they_do_not_track()
    {
        var (owner, ownerId, _) = await api.NewUserAsync();
        var (stranger, _, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        var releaseId = await api.NewReleaseAsync(artistId);
        await api.TrackAsync(ownerId, artistId);

        Assert.Equal(HttpStatusCode.NotFound, (await SetState(stranger, releaseId, "OWNED")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SetState(owner, releaseId, "OWNED")).StatusCode);
        Assert.Equal("[]", await stranger.GetStringAsync("/api/v1/releases?state=OWNED"));
    }

    [Fact]
    public async Task Collection_and_wishlist_are_exclusive_and_private_to_each_user()
    {
        var (owner, ownerId, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        var releaseId = await api.NewReleaseAsync(artistId);
        await api.TrackAsync(ownerId, artistId);

        await SetState(owner, releaseId, "OWNED");
        await SetState(owner, releaseId, "WISHLIST");

        Assert.Equal("[]", await owner.GetStringAsync("/api/v1/releases?state=OWNED"));
        var wishlist = await owner.GetFromJsonAsync<JsonElement>("/api/v1/releases?state=WISHLIST");
        Assert.Equal(releaseId, wishlist[0].GetProperty("id").GetGuid());
        Assert.Equal("Test Artist", wishlist[0].GetProperty("artistName").GetString());
    }

    [Fact]
    public async Task Parallel_state_changes_leave_one_row_and_never_fail()
    {
        var (owner, ownerId, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        var releaseId = await api.NewReleaseAsync(artistId);
        await api.TrackAsync(ownerId, artistId);

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => SetState(owner, releaseId, "OWNED")));

        Assert.All(results, r => Assert.Equal(HttpStatusCode.NoContent, r.StatusCode));
        Assert.Equal(1, (await owner.GetFromJsonAsync<JsonElement>("/api/v1/releases?state=OWNED")).GetArrayLength());
    }

    [Theory]
    [InlineData("HACKED")]
    [InlineData("owned")]
    [InlineData("")]
    public async Task An_unknown_state_is_400(string state)
    {
        var (owner, ownerId, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        var releaseId = await api.NewReleaseAsync(artistId);
        await api.TrackAsync(ownerId, artistId);

        Assert.Equal(HttpStatusCode.BadRequest, (await SetState(owner, releaseId, state)).StatusCode);
    }

    [Fact]
    public async Task Bad_input_is_rejected_before_any_outside_service_is_called()
    {
        var (client, ownerId, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        await api.TrackAsync(ownerId, artistId);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/v1/artists/search?query=%20")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/v1/artists/{artistId}/releases?types=BOGUS")).StatusCode);
        // An artist id that was never returned by a search cannot be added (so the client cannot invent artists).
        var add = await client.PostAsJsonAsync("/api/v1/artists", new { musicbrainzId = Guid.NewGuid().ToString() });
        Assert.Equal(HttpStatusCode.BadRequest, add.StatusCode);
    }
}
