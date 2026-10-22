using CrateWatch.Artists.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Artists.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/artists")]
public class ArtistPhotoController(ArtistPhotoService artistPhotoService) : ControllerBase
{
    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> Photo(Guid id)
    {
        var bytes = await artistPhotoService.GetPhotoAsync(id);
        return bytes is null ? NotFound() : File(bytes, "image/jpeg");
    }
}
