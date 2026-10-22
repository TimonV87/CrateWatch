using CrateWatch.Artists.Repositories;
using CrateWatch.CoverArt;
using CrateWatch.Releases.Repositories;
using CrateWatch.TheAudioDb;

namespace CrateWatch.Artists.Services;

public class ArtistPhotoService(
    ArtistRepository artistRepository,
    ReleaseRepository releaseRepository,
    ArtistPhotoCacheService artistPhotoCacheService,
    CoverArtCacheService coverArtCacheService)
{
    // Null means the artist or any image for it is missing.
    public async Task<byte[]?> GetPhotoAsync(Guid id)
    {
        var artist = await artistRepository.FindByIdAsync(id);
        if (artist is null)
        {
            return null;
        }

        var bytes = await artistPhotoCacheService.GetPhotoAsync(artist);
        if (bytes is not null || artist.CoverReleaseId is not { } coverReleaseId)
        {
            return bytes;
        }

        var release = await releaseRepository.FindByIdAsync(coverReleaseId);
        return release is null ? null : await coverArtCacheService.GetCoverArtAsync(release);
    }
}
