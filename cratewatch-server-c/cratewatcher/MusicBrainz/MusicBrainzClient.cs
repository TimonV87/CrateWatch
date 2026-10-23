using System.Net;
using CrateWatch.MusicBrainz.Domain;

namespace CrateWatch.MusicBrainz;

public class MusicBrainzClient(HttpClient http)
{
    // MusicBrainz allows about 1 request per second, so every call in this process goes through one gate.
    // shortcut: process-wide only; several backend instances would need a shared limiter.
    private static readonly TimeSpan MinGap = TimeSpan.FromMilliseconds(1100);
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime nextCallAt = DateTime.MinValue;

    public Task<MusicBrainzArtistSearchResponse?> SearchArtistsAsync(string query, int limit) =>
        GetAsync<MusicBrainzArtistSearchResponse>(
            $"artist/?query={Uri.EscapeDataString(query)}&limit={limit}&fmt=json");

    public Task<MusicBrainzReleaseGroupSearchResponse?> BrowseReleaseGroupsAsync(string artistMbid, int limit, int offset) =>
        GetAsync<MusicBrainzReleaseGroupSearchResponse>(
            $"release-group?artist={Uri.EscapeDataString(artistMbid)}&limit={limit}&offset={offset}&fmt=json");

    private async Task<T?> GetAsync<T>(string url)
    {
        await Gate.WaitAsync();
        try
        {
            var wait = nextCallAt - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait);
            }
            // MusicBrainz answers 503 when it is busy or has throttled this IP; a short pause usually clears it.
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await http.GetFromJsonAsync<T>(url);
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                }
            }
        }
        finally
        {
            nextCallAt = DateTime.UtcNow + MinGap;
            Gate.Release();
        }
    }
}
