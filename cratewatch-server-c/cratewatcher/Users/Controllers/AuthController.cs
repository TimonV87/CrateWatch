using CrateWatch.Users.Domain;
using CrateWatch.Users.Repositories;
using CrateWatch.Users.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrateWatch.Users.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1")]
public class AuthController(UserRepository userRepository, TokenService tokenService) : ControllerBase
{
    [HttpPost("auth/register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10);
        try
        {
            var saved = await userRepository.SaveAsync(new User { Username = request.Username, PasswordHash = passwordHash });
            return StatusCode(StatusCodes.Status201Created, new RegisterResponse(saved.Id, saved.Username));
        }
        // The unique index on username is the single source of truth, so concurrent registrations cannot both win.
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict();
        }
    }

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
