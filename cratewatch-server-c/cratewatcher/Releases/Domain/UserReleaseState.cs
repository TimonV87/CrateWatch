namespace CrateWatch.Releases.Domain;

public class UserReleaseState
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CdReleaseId { get; set; }

    public string State { get; set; } = "NONE";
}
