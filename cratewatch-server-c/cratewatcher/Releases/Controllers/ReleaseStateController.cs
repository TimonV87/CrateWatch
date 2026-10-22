using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Services;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Releases.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/releases")]
public class ReleaseStateController(ReleaseService releaseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ReleaseResponse>>> ListByState(
        [FromQuery, RegularExpression(SetReleaseStateRequest.StatePattern)] string state) =>
        await releaseService.ListByStateAsync(CurrentUserId, state);

    [HttpPut("{id:guid}/state")]
    public async Task<IActionResult> SetState(Guid id, SetReleaseStateRequest request) =>
        await releaseService.SetStateAsync(CurrentUserId, id, request.State) ? NoContent() : NotFound();

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(TokenService.UserIdClaim)!);
}
