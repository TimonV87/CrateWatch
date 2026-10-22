using CrateWatch.Users.Domain;
using CrateWatch.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrateWatch.Users.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1")]
public class AuthController(AuthService authService) : ControllerBase
{
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var response = await authService.LoginAsync(request);
        return response is null ? Unauthorized() : response;
    }
}
