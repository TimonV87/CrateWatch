using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Services;
using CrateWatch.Releases.Domain;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Artists.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/artists")]
public class ArtistsController(ArtistService artistService, ArtistQueryService artistQueryService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<List<ArtistSearchResult>>> Search([FromQuery, Required(AllowEmptyStrings = false)] string query) =>
        await artistService.SearchAlbumsAsync(query);

    [HttpGet]
    public async Task<ActionResult<List<ArtistResponse>>> List() =>
        await artistQueryService.ListAsync(CurrentUserId);

    [HttpPost]
    public async Task<ActionResult<ArtistResponse>> Create(CreateArtistRequest request)
    {
        try
        {
            var artist = await artistService.CreateArtistAsync(request, CurrentUserId);
            return StatusCode(StatusCodes.Status201Created, ArtistResponse.From(artist));
        }
        catch (ArtistAlreadyExistsException e)
        {
            return Conflict(ArtistResponse.From(e.ExistingArtist));
        }
        catch (ArtistNotSearchedException)
        {
            return Problem("Search for the artist first, then add it.", statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArtistResponse>> Get(Guid id)
    {
        var artist = await artistQueryService.GetAsync(CurrentUserId, id);
        return artist is null ? NotFound() : artist;
    }

    [HttpGet("{id:guid}/releases")]
    public async Task<ActionResult<List<ReleaseResponse>>> Releases(Guid id, [FromQuery] string[]? types)
    {
        try
        {
            var releases = await artistQueryService.GetReleasesAsync(CurrentUserId, id, types);
            return releases is null ? NotFound() : releases;
        }
        catch (UnknownReleaseTypeException e)
        {
            return BadRequest(e.Message);
        }
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(TokenService.UserIdClaim)!);
}
