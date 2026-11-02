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

    // Starts the scan in the background and returns at once; poll GET shops/scan for progress and the result.
    [HttpPost("scan")]
    public ActionResult<ScanStatus> StartScan()
    {
        try
        {
            return Accepted(shopService.StartScan(CurrentUserId));
        }
        catch (ScanInProgressException e)
        {
            return Problem(e.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpGet("scan")]
    public ScanStatus GetScan() => ShopService.GetScanStatus(CurrentUserId);

    [HttpGet("availability")]
    public async Task<ActionResult<List<AvailableRelease>>> Availability() =>
        await shopService.AvailabilityAsync(CurrentUserId);

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(TokenService.UserIdClaim)!);
}
