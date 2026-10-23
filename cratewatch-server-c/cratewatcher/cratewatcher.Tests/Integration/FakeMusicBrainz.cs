using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace CrateWatch.Tests.Integration;

/// <summary>A tiny fake of the two MusicBrainz calls the app makes, with call counting and scripted 503 answers.</summary>
public sealed class FakeMusicBrainz : HttpMessageHandler
{
    public sealed record Group(string Title, string PrimaryType, string[]? SecondaryTypes = null);

    private readonly ConcurrentDictionary<string, List<Group>> catalogs = new();
    private readonly ConcurrentDictionary<(string Artist, int Offset), int> failuresLeft = new();
    private readonly ConcurrentQueue<(string Artist, int Offset)> browseCalls = new();

    public void AddArtist(string mbid, List<Group> groups) => catalogs[mbid] = groups;

    /// <summary>The next <paramref name="times"/> requests for that page answer 503.</summary>
    public void FailPage(string mbid, int offset, int times) => failuresLeft[(mbid, offset)] = times;

    public int BrowseCalls(string mbid, int? offset = null) =>
        browseCalls.Count(c => c.Artist == mbid && (offset is null || c.Offset == offset));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

        if (uri.AbsolutePath.EndsWith("/artist/"))
        {
            // The search text is the MBID of the fake artist, so every test gets exactly the artist it set up.
            var mbid = query["query"]!;
            return Task.FromResult(Json(new { artists = new[] { new { id = mbid, name = "Fake Band", disambiguation = (string?)null, country = "NL" } } }));
        }

        var artist = query["artist"]!;
        var offset = int.Parse(query["offset"]!);
        browseCalls.Enqueue((artist, offset));

        if (failuresLeft.TryGetValue((artist, offset), out var left) && left > 0)
        {
            failuresLeft[(artist, offset)] = left - 1;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        var page = catalogs[artist].Skip(offset).Take(int.Parse(query["limit"]!)).Select((g, i) => new Dictionary<string, object?>
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["title"] = g.Title,
            ["primary-type"] = g.PrimaryType,
            ["secondary-types"] = g.SecondaryTypes ?? [],
            ["first-release-date"] = "1999-01-01",
        });
        return Task.FromResult(Json(new Dictionary<string, object> { ["release-groups"] = page.ToList() }));
    }

    private static HttpResponseMessage Json(object body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json") };
}
