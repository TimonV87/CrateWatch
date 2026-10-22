using CrateWatch.Artists.Repositories;
using CrateWatch.CoverArt;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;

namespace CrateWatch.Releases.Services;

public class ReleaseService(
    ReleaseRepository releaseRepository,
    UserArtistRepository userArtistRepository,
    UserReleaseStateRepository userReleaseStateRepository,
    CoverArtCacheService coverArtCacheService)
{
    public async Task<List<ReleaseResponse>> ListByStateAsync(Guid userId, string state)
    {
        var rows = await userReleaseStateRepository.FindReleasesByStateAsync(userId, state);
        return rows.Select(x => ReleaseResponse.From(x.Release, state, x.ArtistName)).ToList();
    }

    // Returns false when the release does not exist or the user does not track its artist.
    public async Task<bool> SetStateAsync(Guid userId, Guid releaseId, string state)
    {
        var release = await releaseRepository.FindByIdAsync(releaseId);
        if (release is null || !await userArtistRepository.ExistsByUserIdAndArtistIdAsync(userId, release.ArtistId))
        {
            return false;
        }

        await userReleaseStateRepository.SetAsync(userId, releaseId, state);
        return true;
    }

    public async Task<byte[]?> GetCoverArtAsync(Guid releaseId)
    {
        var release = await releaseRepository.FindByIdAsync(releaseId);
        return release is null ? null : await coverArtCacheService.GetCoverArtAsync(release);
    }
}
