using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Repositories;
using CrateWatch.MusicBrainz;
using CrateWatch.MusicBrainz.Domain;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;
using CrateWatch.Releases.Services;
using Microsoft.Extensions.Caching.Memory;

namespace CrateWatch.Artists.Services;

public class ArtistImportService(
    MusicBrainzClient musicBrainzClient,
    ArtistRepository artistRepository,
    ReleaseRepository releaseRepository,
    IMemoryCache cache)
{
    private const int PageSize = 100;
    // shortcut: reads at most 25 pages = 2,500 release groups (about 27 s); raise it if an artist has more.
    private const int MaxPages = 25;
    private static readonly TimeSpan PageCacheTtl = TimeSpan.FromMinutes(10);

    // Studio albums are imported when an artist is added; the other types on demand (see CONTEXT.md decisions log).
    public const string StudioAlbum = "STUDIO_ALBUM";
    public static readonly string[] OptionalTypes = ["EP", "COMPILATION", "LIVE", "SOUNDTRACK", "SINGLE", "OTHER"];

    public async Task<Artist> ImportReleasesAsync(Artist artist)
    {
        await ImportTypesAsync(artist, [StudioAlbum]);
        return await AssignCoverReleaseAsync(artist);
    }

    public async Task ImportTypesAsync(Artist artist, ICollection<string> types)
    {
        var knownTitles = await releaseRepository.FindTitlesByArtistIdAsync(artist.Id);
        var newReleases = new List<CdRelease>();

        for (var page = 0; page < MaxPages; page++)
        {
            var releaseGroups = await GetPageAsync(artist.MusicbrainzId, page * PageSize);

            foreach (var releaseGroup in releaseGroups)
            {
                var releaseType = ReleaseTypeClassifier.Classify(releaseGroup.PrimaryType, releaseGroup.SecondaryTypes);
                // HashSet.Add is false for a title we already have, in the database or earlier in this run.
                if (types.Contains(releaseType) && knownTitles.Add(releaseGroup.Title))
                {
                    newReleases.Add(new CdRelease
                    {
                        ArtistId = artist.Id,
                        Title = releaseGroup.Title,
                        ReleaseType = releaseType,
                        FirstReleaseDate = ParseReleaseDate(releaseGroup.FirstReleaseDate),
                        MusicbrainzReleaseGroupId = releaseGroup.Id,
                    });
                }
            }

            if (releaseGroups.Count < PageSize)
            {
                break;
            }
        }

        await releaseRepository.SaveAllAsync(newReleases);
    }

    // A page is kept for a few minutes: turning on another type reuses it, and a retry after a MusicBrainz error
    // resumes at the page that failed. A failed fetch is never cached.
    private async Task<List<MusicBrainzReleaseGroup>> GetPageAsync(string artistMbid, int offset) =>
        (await cache.GetOrCreateAsync($"mb-release-groups:{artistMbid}:{offset}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PageCacheTtl;
            var response = await musicBrainzClient.BrowseReleaseGroupsAsync(artistMbid, PageSize, offset);
            return response?.ReleaseGroups ?? [];
        }))!;

    /// <summary>
    /// MusicBrainz dates have variable precision: "", "YYYY", "YYYY-MM", or "YYYY-MM-DD".
    /// Missing month/day are normalized to 1 rather than tracked separately.
    /// </summary>
    internal static DateOnly? ParseReleaseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var parts = raw.Split('-');
        if (!int.TryParse(parts[0], out var year))
        {
            return null;
        }

        var month = 1;
        var day = 1;
        
        switch (parts.Length)
        {
            case > 1 when !int.TryParse(parts[1], out month):
            case > 2 when !int.TryParse(parts[2], out day):
                return null;
            default:
                try
                {
                    return new DateOnly(year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
        }
    }

    private async Task<Artist> AssignCoverReleaseAsync(Artist artist)
    {
        var releases = await releaseRepository.FindByArtistIdAsync(artist.Id);

        var cover = releases.FirstOrDefault(r => r.MusicbrainzReleaseGroupId is not null);

        if (cover is not null)
        {
            artist.CoverReleaseId = cover.Id;
        }

        return await artistRepository.UpdateAsync(artist);
    }
}
