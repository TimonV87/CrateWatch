using System.Text.Json.Serialization;

namespace CrateWatch.MusicBrainz.Domain;

public record MusicBrainzArtist(string Id, string Name, string? Disambiguation, string? Country);

public record MusicBrainzArtistSearchResponse(List<MusicBrainzArtist>? Artists);

public record MusicBrainzReleaseGroup(
    string Id,
    string Title,
    [property: JsonPropertyName("primary-type")] string? PrimaryType,
    [property: JsonPropertyName("secondary-types")] List<string>? SecondaryTypes,
    [property: JsonPropertyName("first-release-date")] string? FirstReleaseDate
);

public record MusicBrainzReleaseGroupSearchResponse(
    [property: JsonPropertyName("release-groups")] List<MusicBrainzReleaseGroup>? ReleaseGroups
);
