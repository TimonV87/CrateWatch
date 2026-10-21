using CrateWatch.MusicBrainz.Domain;

namespace CrateWatch.MusicBrainz;

public class MusicBrainzClient(HttpClient http)
{
    public Task<MusicBrainzArtistSearchResponse?> SearchArtistsAsync(string query, int limit) =>
        http.GetFromJsonAsync<MusicBrainzArtistSearchResponse>(
            $"artist/?query={Uri.EscapeDataString(query)}&limit={limit}&fmt=json");

    public Task<MusicBrainzReleaseGroupSearchResponse?> BrowseReleaseGroupsAsync(string artistMbid, int limit, int offset) =>
        http.GetFromJsonAsync<MusicBrainzReleaseGroupSearchResponse>(
            $"release-group?artist={Uri.EscapeDataString(artistMbid)}&limit={limit}&offset={offset}&fmt=json");
}
