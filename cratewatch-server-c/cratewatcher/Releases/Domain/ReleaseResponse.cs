namespace CrateWatch.Releases.Domain;

public record ReleaseResponse(
    Guid Id,
    string Title,
    string ReleaseType,
    string State,
    string? CoverArtUrl,
    DateOnly? FirstReleaseDate,
    Guid ArtistId,
    string? ArtistName = null)
{
    public static ReleaseResponse From(CdRelease release, string state, string? artistName = null) =>
        new(
            release.Id,
            release.Title,
            release.ReleaseType,
            state,
            $"/releases/{release.Id}/cover-art",
            release.FirstReleaseDate,
            release.ArtistId,
            artistName);
}
