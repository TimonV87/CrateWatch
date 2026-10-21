using System.Net;
using CrateWatch.Caching;
using CrateWatch.Configuration;
using CrateWatch.Releases.Domain;

namespace CrateWatch.CoverArt;

public class CoverArtCacheService(
    CoverArtArchiveClient coverArtArchiveClient,
    StoragePaths storagePaths,
    ImageFileCache cache)
{
    public Task<byte[]?> GetCoverArtAsync(CdRelease release) =>
        release.MusicbrainzReleaseGroupId is { } mbid
            ? cache.GetAsync(storagePaths.CoverArt, release.Id, () => FetchAsync(mbid))
            : Task.FromResult<byte[]?>(null);

    private async Task<byte[]?> FetchAsync(string mbid)
    {
        try
        {
            return await coverArtArchiveClient.FetchFrontCoverAsync(mbid);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
