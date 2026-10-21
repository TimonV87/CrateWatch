namespace CrateWatch.Releases.Domain;

public class CdRelease
{
    public Guid Id { get; set; }

    public Guid ArtistId { get; set; }

    public required string Title { get; set; }

    public required string ReleaseType { get; set; }

    public DateOnly? FirstReleaseDate { get; set; }

    public string? MusicbrainzReleaseGroupId { get; set; }

    public string? CoverArtUrl { get; set; }
}
