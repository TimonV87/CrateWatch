using CrateWatch.Artists.Domain;

namespace CrateWatch.Artists.Services;

public class ArtistAlreadyExistsException(Artist existingArtist)
    : Exception($"Artist already added: {existingArtist.Id}")
{
    public Artist ExistingArtist { get; } = existingArtist;
}
