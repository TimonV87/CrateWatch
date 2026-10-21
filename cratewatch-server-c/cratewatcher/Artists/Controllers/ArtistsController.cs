using System.Security.Claims;
using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Repositories;
using CrateWatch.Artists.Services;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Artists.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/artists")]
public class ArtistsController(
    ArtistRepository artistRepository,
    UserArtistRepository userArtistRepository,
    ArtistService artistService,
    UserReleaseStateRepository userReleaseStateRepository) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<List<ArtistSearchResult>>> Search([FromQuery] string query) =>
        await artistService.SearchAlbumsAsync(query);

    [HttpGet]
    public async Task<ActionResult<List<ArtistResponse>>> List()
    {
        var artistIds = (await userArtistRepository.FindByUserIdAsync(CurrentUserId))
            .Select(ua => ua.ArtistId)
            .ToList();
        var artists = await artistRepository.FindByIdInAsync(artistIds);
        return artists.Select(ArtistResponse.From).ToList();
    }

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
        var artist = await FindOwnedArtistAsync(id);
        return artist is null ? NotFound() : ArtistResponse.From(artist);
    }

    [HttpGet("{id:guid}/releases")]
    public async Task<ActionResult<List<ReleaseResponse>>> Releases(Guid id, [FromQuery] string[]? types)
    {
        // Default view is studio albums; other types are fetched on demand when the UI switches them on.
        string[] requested = types is { Length: > 0 } ? types : [ArtistImportService.StudioAlbum];
        if (requested.Any(t => t != ArtistImportService.StudioAlbum && !ArtistImportService.OptionalTypes.Contains(t)))
        {
            return BadRequest("Unknown release type.");
        }

        var artist = await FindOwnedArtistAsync(id);
        if (artist is null)
        {
            return NotFound();
        }

        var releases = await artistService.GetReleasesAsync(artist, requested);
        var releaseIds = releases.Select(r => r.Id).ToList();

        var states = (await userReleaseStateRepository.FindByUserIdAndCdReleaseIdInAsync(CurrentUserId, releaseIds))
            .ToDictionary(s => s.CdReleaseId, s => s.State);

        return releases
            .Select(r => ReleaseResponse.From(r, states.GetValueOrDefault(r.Id, "NONE")))
            .ToList();
    }

    private async Task<Artist?> FindOwnedArtistAsync(Guid id)
    {
        var artist = await artistRepository.FindByIdAsync(id);
        if (artist is null || !await userArtistRepository.ExistsByUserIdAndArtistIdAsync(CurrentUserId, id))
        {
            return null;
        }
        return artist;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(TokenService.UserIdClaim)!);
}
