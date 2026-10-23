using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Repositories;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;
using CrateWatch.MusicBrainz;
using Microsoft.Extensions.Caching.Memory;

namespace CrateWatch.Artists.Services;

public class ArtistService(
    MusicBrainzClient musicBrainzClient,
    ArtistRepository artistRepository,
    UserArtistRepository userArtistRepository,
    ArtistImportService artistImportService,
    ReleaseRepository releaseRepository,
    IMemoryCache cache,
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

    // Fetches the requested non-studio types from MusicBrainz the first time they are asked for, then returns the stored releases of those types.
    public async Task<List<CdRelease>> GetReleasesAsync(Artist artist, ICollection<string> types)
    {
        var missing = types.Where(t => t != ArtistImportService.StudioAlbum && !cache.TryGetValue(ImportedKey(artist.Id, t), out _)).ToList();
        if (missing.Count > 0)
        {
            var gate = CreateLocks[(uint)artist.MusicbrainzId.GetHashCode() % CreateLocks.Length];
            await gate.WaitAsync();
            try
            {
                // Re-check: a concurrent request may have imported these while we waited for the lock.
                missing = missing.Where(t => !cache.TryGetValue(ImportedKey(artist.Id, t), out _)).ToList();
                if (missing.Count > 0)
                {
                    await artistImportService.ImportTypesAsync(artist, missing);
                    // shortcut: "already imported" lives in process memory; after a restart/24h the next switch-on repeats one idempotent MusicBrainz browse.
                    foreach (var type in missing)
                    {
                        cache.Set(ImportedKey(artist.Id, type), true, TimeSpan.FromHours(24));
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        }

        return await releaseRepository.FindByArtistIdAndTypesOrderByFirstReleaseDateAsync(artist.Id, types.ToArray());
    }

    private static string ImportedKey(Guid artistId, string type) => $"imported:{artistId}:{type}";

    private async Task<Artist> ImportNewArtistAsync(string mbid)
    {
        // shortcut: search results live in process memory, so a restart (or >1h) means "search again"; use a MusicBrainz lookup by id if that gets annoying.
        if (!cache.TryGetValue(SearchKey(mbid), out ArtistSearchResult? found) || found is null)
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
            cache.Set(SearchKey(result.MusicbrainzId), result, SearchResultTtl);
        }

        return results;
    }

    private static string SearchKey(string mbid) => $"mb-artist:{mbid}";
}
