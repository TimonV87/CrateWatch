namespace CrateWatch.Artists.Services;

public class ArtistNotSearchedException(string musicbrainzId)
    : Exception($"Artist {musicbrainzId} was not found in a recent search")
{
}
