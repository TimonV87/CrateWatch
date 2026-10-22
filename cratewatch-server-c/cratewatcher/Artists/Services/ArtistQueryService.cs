using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Repositories;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;

namespace CrateWatch.Artists.Services;

public class ArtistQueryService(
    ArtistRepository artistRepository,
    UserArtistRepository userArtistRepository,
    UserReleaseStateRepository userReleaseStateRepository,
    ArtistService artistService)
{
    public async Task<List<ArtistResponse>> ListAsync(Guid userId)
    {
        var artistIds = (await userArtistRepository.FindByUserIdAsync(userId))
            .Select(ua => ua.ArtistId)
            .ToList();
        var artists = await artistRepository.FindByIdInAsync(artistIds);
        return artists.Select(ArtistResponse.From).ToList();
    }

    public async Task<ArtistResponse?> GetAsync(Guid userId, Guid id)
    {
        var artist = await FindOwnedArtistAsync(userId, id);
        return artist is null ? null : ArtistResponse.From(artist);
    }

    // Returns null when the user does not track the artist.
    public async Task<List<ReleaseResponse>?> GetReleasesAsync(Guid userId, Guid id, string[]? types)
    {
        // Default view is studio albums; other types are fetched on demand when the UI switches them on.
        string[] requested = types is { Length: > 0 } ? types : [ArtistImportService.StudioAlbum];
        if (requested.Any(t => t != ArtistImportService.StudioAlbum && !ArtistImportService.OptionalTypes.Contains(t)))
        {
            throw new UnknownReleaseTypeException();
        }

        var artist = await FindOwnedArtistAsync(userId, id);
        if (artist is null)
        {
            return null;
        }

        var releases = await artistService.GetReleasesAsync(artist, requested);
        var releaseIds = releases.Select(r => r.Id).ToList();

        var states = (await userReleaseStateRepository.FindByUserIdAndCdReleaseIdInAsync(userId, releaseIds))
            .ToDictionary(s => s.CdReleaseId, s => s.State);

        return releases
            .Select(r => ReleaseResponse.From(r, states.GetValueOrDefault(r.Id, "NONE")))
            .ToList();
    }

    private async Task<Artist?> FindOwnedArtistAsync(Guid userId, Guid id)
    {
        var artist = await artistRepository.FindByIdAsync(id);
        if (artist is null || !await userArtistRepository.ExistsByUserIdAndArtistIdAsync(userId, id))
        {
            return null;
        }
        return artist;
    }
}
