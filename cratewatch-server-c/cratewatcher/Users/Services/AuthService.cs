using CrateWatch.Users.Domain;
using CrateWatch.Users.Repositories;
using CrateWatch.Users.Security;

namespace CrateWatch.Users.Services;

public class AuthService(UserRepository userRepository, TokenService tokenService)
{
    // Returns null on unknown user or wrong password.
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.FindByUsernameAsync(request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = tokenService.CreateToken(user.Username);
        return new LoginResponse(user.Username, token, "Bearer", TokenService.ExpiresInSeconds);
    }
}
