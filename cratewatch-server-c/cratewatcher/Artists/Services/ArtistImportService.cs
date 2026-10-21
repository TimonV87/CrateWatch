using CrateWatch.Artists.Domain;
using CrateWatch.Artists.Repositories;
using CrateWatch.MusicBrainz;
using CrateWatch.MusicBrainz.Domain;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;
using CrateWatch.Releases.Services;

namespace CrateWatch.Artists.Services;

public class ArtistImportService(
    MusicBrainzClient musicBrainzClient,
    ArtistRepository artistRepository,
    ReleaseRepository releaseRepository
    )
{
    private const int PageSize = 100;
    private const int MaxPages = 5;
    private static readonly TimeSpan RateLimitDelay = TimeSpan.FromMilliseconds(1100);

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
        var offset = 0;
        for (var page = 0; page < MaxPages; page++)
        {
            if (page > 0)
            {
                await Task.Delay(RateLimitDelay);
            }

            var response = await musicBrainzClient.BrowseReleaseGroupsAsync(artist.MusicbrainzId, PageSize, offset);
            var releaseGroups = response?.ReleaseGroups ?? [];

            foreach (var releaseGroup in releaseGroups)
            {
                await ImportReleaseGroupAsync(artist, releaseGroup, types);
            }

            if (releaseGroups.Count < PageSize)
            {
                break;
            }
            offset += PageSize;
        }
    }

    private async Task ImportReleaseGroupAsync(Artist artist, MusicBrainzReleaseGroup releaseGroup, ICollection<string> types)
    {
        var releaseType = ReleaseTypeClassifier.Classify(releaseGroup.PrimaryType, releaseGroup.SecondaryTypes);
        if (!types.Contains(releaseType))
        {
            return;
        }
        if (await releaseRepository.ExistsByArtistIdAndTitleAsync(artist.Id, releaseGroup.Title))
        {
            return;
        }

        await releaseRepository.SaveAsync(new CdRelease
        {
            ArtistId = artist.Id,
            Title = releaseGroup.Title,
            ReleaseType = releaseType,
            FirstReleaseDate = ParseReleaseDate(releaseGroup.FirstReleaseDate),
            MusicbrainzReleaseGroupId = releaseGroup.Id,
            CoverArtUrl = $"https://coverartarchive.org/release-group/{releaseGroup.Id}/front-250",
        });
    }

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
