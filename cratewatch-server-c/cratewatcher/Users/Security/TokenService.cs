using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CrateWatch.Users.Security;

public class TokenService(SymmetricSecurityKey signingKey)
{
    public const int ExpiresInSeconds = 3600;
    public const string UserIdClaim = "uid";

    public string CreateToken(string username)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, username)]),
            Expires = DateTime.UtcNow.AddSeconds(ExpiresInSeconds),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
