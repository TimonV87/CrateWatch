using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CrateWatch.Artists.Repositories;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Releases.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/releases")]
public class ReleaseStateController(
    ReleaseRepository releaseRepository,
    UserArtistRepository userArtistRepository,
    UserReleaseStateRepository userReleaseStateRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ReleaseResponse>>> ListByState(
        [FromQuery, RegularExpression(SetReleaseStateRequest.StatePattern)] string state)
    {
        var rows = await userReleaseStateRepository.FindReleasesByStateAsync(CurrentUserId, state);
        return rows.Select(x => ReleaseResponse.From(x.Release, state, x.ArtistName)).ToList();
    }

    [HttpPut("{id:guid}/state")]
    public async Task<IActionResult> SetState(Guid id, SetReleaseStateRequest request)
    {
        var release = await releaseRepository.FindByIdAsync(id);
        if (release is null || !await userArtistRepository.ExistsByUserIdAndArtistIdAsync(CurrentUserId, release.ArtistId))
        {
            return NotFound();
        }

        await userReleaseStateRepository.SetAsync(CurrentUserId, id, request.State);
        return NoContent();
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(TokenService.UserIdClaim)!);
}
