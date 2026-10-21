namespace CrateWatch.TheAudioDb.Domain;

public record TheAudioDbArtist(string? StrArtistThumb);

public record TheAudioDbLookupResponse(List<TheAudioDbArtist>? Artists);
