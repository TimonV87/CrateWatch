namespace CrateWatch.CoverArt;

public class CoverArtArchiveClient(HttpClient http)
{
    public async Task<byte[]> FetchFrontCoverAsync(string mbid)
    {
        using var response = await http.GetAsync($"release-group/{Uri.EscapeDataString(mbid)}/front-250");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
