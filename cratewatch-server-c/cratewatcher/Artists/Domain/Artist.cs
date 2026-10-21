namespace CrateWatch.Artists.Domain;

public class Artist
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string MusicbrainzId { get; set; }

    public Guid? CoverReleaseId { get; set; }
}
