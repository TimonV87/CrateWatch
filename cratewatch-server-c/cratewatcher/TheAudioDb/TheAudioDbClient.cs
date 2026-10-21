using CrateWatch.TheAudioDb.Domain;

namespace CrateWatch.TheAudioDb;

public class TheAudioDbClient(HttpClient http)
{
    public Task<TheAudioDbLookupResponse?> LookupByMbidAsync(string mbid) =>
        http.GetFromJsonAsync<TheAudioDbLookupResponse>($"artist-mb.php?i={Uri.EscapeDataString(mbid)}");
}
