using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static CrateWatch.Tests.Integration.FakeMusicBrainz;

namespace CrateWatch.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ImportTests(ApiFixture api)
{
    // Adding an artist takes two steps, like in the app: search (caches the result), then add.
    private static async Task<HttpResponseMessage> AddArtistAsync(HttpClient client, string mbid)
    {
        (await client.GetAsync($"/api/v1/artists/search?query={mbid}")).EnsureSuccessStatusCode();
        return await client.PostAsJsonAsync("/api/v1/artists", new { musicbrainzId = mbid });
    }

    private static async Task<string[]> TitlesAsync(HttpClient client, Guid artistId, string query = "")
    {
        var releases = await client.GetFromJsonAsync<JsonElement>($"/api/v1/artists/{artistId}/releases{query}");
        return releases.EnumerateArray().Select(r => r.GetProperty("title").GetString()!).Order().ToArray();
    }

    [Fact]
    public async Task Adding_an_artist_imports_studio_albums_and_extra_types_cost_no_more_musicbrainz_calls()
    {
        var mbid = Guid.NewGuid().ToString();
        api.MusicBrainz.AddArtist(mbid,
        [
            new("First LP", "Album"), new("Second LP", "Album"),
            new("An EP", "EP"), new("Other EP", "EP"), new("Other EP", "EP"), // same title twice: stored once
            new("Live Thing", "Album", ["Live"]), new("A Single", "Single"),
        ]);
        var (client, _, _) = await api.NewUserAsync();

        var created = await AddArtistAsync(client, mbid);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var artistId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        Assert.Equal(["First LP", "Second LP"], await TitlesAsync(client, artistId));
        Assert.Equal(1, api.MusicBrainz.BrowseCalls(mbid));

        Assert.Equal(["An EP", "Other EP"], await TitlesAsync(client, artistId, "?types=EP"));
        Assert.Equal(["An EP", "Live Thing", "Other EP"], await TitlesAsync(client, artistId, "?types=EP&types=LIVE"));
        Assert.Equal(["An EP", "Other EP"], await TitlesAsync(client, artistId, "?types=EP"));
        // The page read when the artist was added was reused every time; MusicBrainz was asked once in total.
        Assert.Equal(1, api.MusicBrainz.BrowseCalls(mbid));
    }

    [Fact]
    public async Task A_musicbrainz_503_is_retried_and_the_request_still_succeeds()
    {
        var mbid = Guid.NewGuid().ToString();
        api.MusicBrainz.AddArtist(mbid, [new("Only LP", "Album")]);
        api.MusicBrainz.FailPage(mbid, offset: 0, times: 1);
        var (client, _, _) = await api.NewUserAsync();

        var created = await AddArtistAsync(client, mbid);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(2, api.MusicBrainz.BrowseCalls(mbid)); // one 503, then the retry
    }

    [Fact]
    public async Task After_a_failed_import_a_retry_resumes_at_the_page_that_failed()
    {
        var mbid = Guid.NewGuid().ToString();
        // 100 singles fill page 1, so the studio albums sit on page 2.
        var groups = Enumerable.Range(1, 100).Select(i => new Group($"Single {i}", "Single")).ToList();
        groups.Add(new Group("Late LP", "Album"));
        api.MusicBrainz.AddArtist(mbid, groups);
        api.MusicBrainz.FailPage(mbid, offset: 100, times: 3); // all 3 tries of the first request fail
        var (client, _, _) = await api.NewUserAsync();

        var first = await AddArtistAsync(client, mbid);
        Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/v1/artists", new { musicbrainzId = mbid });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var artistId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(["Late LP"], await TitlesAsync(client, artistId));

        Assert.Equal(1, api.MusicBrainz.BrowseCalls(mbid, offset: 0));    // page 1 was not read a second time
        Assert.Equal(4, api.MusicBrainz.BrowseCalls(mbid, offset: 100));  // 3 failed tries, then the good one
    }
}
