using CrateWatch.Users.Domain;
using CrateWatch.Users.Repositories;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CrateWatch.Users.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1")]
public class AuthController(UserRepository userRepository, TokenService tokenService) : ControllerBase
{
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await userRepository.FindByUsernameAsync(request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized();
        }

        var token = tokenService.CreateToken(user.Username);
        return new LoginResponse(user.Username, token, "Bearer", TokenService.ExpiresInSeconds);
    }
}
