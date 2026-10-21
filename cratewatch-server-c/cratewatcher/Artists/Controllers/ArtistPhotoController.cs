using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Repositories;
using CrateWatch.CoverArt;
using CrateWatch.Releases.Repositories;
using CrateWatch.TheAudioDb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Artists.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/artists")]
public class ArtistPhotoController(
    ArtistRepository artistRepository,
    ReleaseRepository releaseRepository,
    ArtistPhotoCacheService artistPhotoCacheService,
    CoverArtCacheService coverArtCacheService) : ControllerBase
{
    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> Photo(Guid id)
    {
        var artist = await artistRepository.FindByIdAsync(id);
        if (artist is null)
        {
            return NotFound();
        }

        var bytes = await artistPhotoCacheService.GetPhotoAsync(artist)
                    ?? await CoverFallbackAsync(artist);

        return bytes is null ? NotFound() : File(bytes, "image/jpeg");
    }

    private async Task<byte[]?> CoverFallbackAsync(Artist artist)
    {
        if (artist.CoverReleaseId is not { } coverReleaseId)
        {
            return null;
        }

        var release = await releaseRepository.FindByIdAsync(coverReleaseId);
        return release is null ? null : await coverArtCacheService.GetCoverArtAsync(release);
    }
}
