using System.ComponentModel.DataAnnotations;

namespace CrateWatch.Artists.Domain;

public record ArtistResponse(Guid Id, string Name, string MusicbrainzId, string? CoverArtUrl)
{
    public static ArtistResponse From(Artist artist) =>
        new(artist.Id, artist.Name, artist.MusicbrainzId, $"/artists/{artist.Id}/photo");
}

public record ArtistSearchResult(string MusicbrainzId, string Name, string? Disambiguation, string? Country);

// The artist name is deliberately not accepted from the client; it comes from the MusicBrainz search result.
public record CreateArtistRequest(
    [Required(AllowEmptyStrings = false), StringLength(36)] string MusicbrainzId
);
