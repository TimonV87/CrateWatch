using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Repositories;
using CrateWatch.MusicBrainz;
using Microsoft.Extensions.Caching.Memory;

namespace CrateWatch.Artists.Services;

public class ArtistService(
    MusicBrainzClient musicBrainzClient,
    ArtistRepository artistRepository,
    UserArtistRepository userArtistRepository,
    ArtistImportService artistImportService,
    IMemoryCache searchCache,
    ILogger<ArtistService> log)
{
    private static readonly TimeSpan SearchResultTtl = TimeSpan.FromHours(1);
    private const int MaxNameLength = 255;

    // shortcut: in-process lock, only safe with one backend instance; use pg_advisory_lock if we ever scale out.
    // Striped (not per-MBID) so clients cannot grow it by posting random ids.
    private static readonly SemaphoreSlim[] CreateLocks =
        Enumerable.Range(0, 16).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task<Artist> CreateArtistAsync(CreateArtistRequest request, Guid userId)
    {
        // Serializes same-MBID requests so a second caller waits for the first import to finish
        // instead of hitting the unique index or linking a half-imported artist.
        var mbid = request.MusicbrainzId.ToLowerInvariant();
        var gate = CreateLocks[(uint)mbid.GetHashCode() % CreateLocks.Length];
        await gate.WaitAsync();
        try
        {
            var artist = await artistRepository.FindByMusicbrainzIdAsync(mbid)
                         ?? await ImportNewArtistAsync(mbid);

            if (await userArtistRepository.ExistsByUserIdAndArtistIdAsync(userId, artist.Id))
            {
                throw new ArtistAlreadyExistsException(artist);
            }

            await userArtistRepository.SaveAsync(new UserArtist { UserId = userId, ArtistId = artist.Id });
            return artist;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<Artist> ImportNewArtistAsync(string mbid)
    {
        // shortcut: search results live in process memory, so a restart (or >1h) means "search again"; use a MusicBrainz lookup by id if that gets annoying.
        if (!searchCache.TryGetValue(SearchKey(mbid), out ArtistSearchResult? found) || found is null)
        {
            throw new ArtistNotSearchedException(mbid);
        }

        var name = found.Name.Length > MaxNameLength ? found.Name[..MaxNameLength] : found.Name;
        var artist = await artistRepository.SaveAsync(new Artist { Name = name, MusicbrainzId = mbid });

        try
        {
            return await artistImportService.ImportReleasesAsync(artist);
        }
        catch
        {
            await artistRepository.DeleteAsync(artist);
            throw;
        }
    }

    public async Task<List<ArtistSearchResult>> SearchAlbumsAsync(string artistName)
    {
        var response = await musicBrainzClient.SearchArtistsAsync(artistName, 15);

        if (response is null)
        {
            log.LogError("MusicBrainz search returned a null response body for query '{Query}'", artistName);
            return [];
        }

        var results = (response.Artists ?? [])
            .Select(a => new ArtistSearchResult(a.Id.ToLowerInvariant(), a.Name, a.Disambiguation, a.Country))
            .ToList();

        foreach (var result in results)
        {
            searchCache.Set(SearchKey(result.MusicbrainzId), result, SearchResultTtl);
        }

        return results;
    }

    private static string SearchKey(string mbid) => $"mb-artist:{mbid}";
}
