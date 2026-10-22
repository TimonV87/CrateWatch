using CrateWatch.Releases.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Releases.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/releases")]
public class CoverArtController(ReleaseService releaseService) : ControllerBase
{
    [HttpGet("{id:guid}/cover-art")]
    public async Task<IActionResult> CoverArt(Guid id)
    {
        var bytes = await releaseService.GetCoverArtAsync(id);
        return bytes is null ? NotFound() : File(bytes, "image/jpeg");
    }
}
