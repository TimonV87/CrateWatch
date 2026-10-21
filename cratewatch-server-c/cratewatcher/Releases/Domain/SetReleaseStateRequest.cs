using System.ComponentModel.DataAnnotations;

namespace CrateWatch.Releases.Domain;

public record SetReleaseStateRequest(
    [Required, RegularExpression(SetReleaseStateRequest.StatePattern)] string State
)
{
    public const string StatePattern = "^(NONE|WISHLIST|OWNED|DONT_WANT)$";
}
