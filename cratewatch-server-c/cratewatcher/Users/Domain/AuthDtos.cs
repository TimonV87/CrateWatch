using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CrateWatch.Users.Domain;

public record RegisterRequest(
    [Required(AllowEmptyStrings = false), StringLength(100)] string Username,
    [Required(AllowEmptyStrings = false), StringLength(int.MaxValue, MinimumLength = 8)] string Password
);

public record RegisterResponse(Guid Id, string Username);

public record LoginRequest(
    [Required(AllowEmptyStrings = false)] string Username,
    [Required(AllowEmptyStrings = false)] string Password
);

public record LoginResponse(
    string Username,
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn
);
