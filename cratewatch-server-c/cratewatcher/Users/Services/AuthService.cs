using CrateWatch.Users.Domain;
using CrateWatch.Users.Repositories;
using CrateWatch.Users.Security;

namespace CrateWatch.Users.Services;

public class AuthService(UserRepository userRepository, TokenService tokenService)
{
    // Checked even for unknown users, so the response time does not reveal which usernames exist.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("not-a-real-password", workFactor: 10);

    // Returns null on unknown user or wrong password.
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.FindByUsernameAsync(request.Username);
        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);
        if (user is null || !passwordOk)
        {
            return null;
        }

        var token = tokenService.CreateToken(user.Username);
        return new LoginResponse(user.Username, token, "Bearer", TokenService.ExpiresInSeconds);
    }
}
