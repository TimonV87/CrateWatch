using System.Net;
using CrateWatch.Artists.Domain;
using CrateWatch.Caching;
using CrateWatch.Configuration;

namespace CrateWatch.TheAudioDb;

public class ArtistPhotoCacheService(
    TheAudioDbClient theAudioDbClient,
    IHttpClientFactory httpClientFactory,
    StoragePaths storagePaths,
    ImageFileCache cache)
{
    public const string ImageHttpClientName = "artist-photo-download";

    public Task<byte[]?> GetPhotoAsync(Artist artist) =>
        cache.GetAsync(storagePaths.ArtistPhoto, artist.Id, () => FetchAsync(artist));

    private async Task<byte[]?> FetchAsync(Artist artist)
    {
        var response = await theAudioDbClient.LookupByMbidAsync(artist.MusicbrainzId);
        var thumbUrl = response?.Artists?.FirstOrDefault()?.StrArtistThumb;
        return IsAllowedImageUrl(thumbUrl) ? await DownloadImageAsync(thumbUrl!) : null;
    }

    // The URL comes from a third-party JSON; never let it point the server at our own network.
    private static bool IsAllowedImageUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && (uri.Host == "theaudiodb.com" || uri.Host.EndsWith(".theaudiodb.com", StringComparison.OrdinalIgnoreCase));

    private async Task<byte[]?> DownloadImageAsync(string url)
    {
        using var response = await httpClientFactory.CreateClient(ImageHttpClientName).GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
