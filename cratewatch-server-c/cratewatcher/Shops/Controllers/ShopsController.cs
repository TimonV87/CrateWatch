using System.Security.Claims;
using CrateWatch.Shops.Domain;
using CrateWatch.Shops.Services;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrateWatch.Shops.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/shops")]
public class ShopsController(ShopService shopService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ShopResponse>>> List() =>
        await shopService.ListAsync(CurrentUserId);

    [HttpPost]
    public async Task<ActionResult<ShopResponse>> Create(CreateShopRequest request, CancellationToken ct)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await shopService.AddAsync(CurrentUserId, request, ct));
        }
        catch (InvalidShopException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) =>
        await shopService.DeleteAsync(CurrentUserId, id) ? NoContent() : NotFound();

    [HttpPost("scan")]
    public async Task<ActionResult<ScanResult>> Scan(CancellationToken ct)
    {
        try
        {
            return await shopService.ScanAsync(CurrentUserId, ct);
        }
        catch (ScanInProgressException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpGet("availability")]
    public async Task<ActionResult<List<AvailableRelease>>> Availability() =>
        await shopService.AvailabilityAsync(CurrentUserId);

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(TokenService.UserIdClaim)!);
}
