using CrateWatch.CoverArt;
using CrateWatch.Releases.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Releases.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/releases")]
public class CoverArtController(
    ReleaseRepository releaseRepository,
    CoverArtCacheService coverArtCacheService) : ControllerBase
{
    [HttpGet("{id:guid}/cover-art")]
    public async Task<IActionResult> CoverArt(Guid id)
    {
        var release = await releaseRepository.FindByIdAsync(id);
        if (release is null)
        {
            return NotFound();
        }

        var bytes = await coverArtCacheService.GetCoverArtAsync(release);
        return bytes is null ? NotFound() : File(bytes, "image/jpeg");
    }
}
