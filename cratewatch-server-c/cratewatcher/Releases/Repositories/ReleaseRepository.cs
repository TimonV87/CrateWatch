using CrateWatch.Db;
using CrateWatch.Releases.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrateWatch.Releases.Repositories;

public class ReleaseRepository(CrateWatchDbContext db)
{
    public Task<CdRelease?> FindByIdAsync(Guid id) =>
        db.CdReleases.FirstOrDefaultAsync(r => r.Id == id);

    public Task<List<CdRelease>> FindByArtistIdAsync(Guid artistId) =>
        db.CdReleases.Where(r => r.ArtistId == artistId).ToListAsync();

    // Postgres sorts NULLs last for ascending order, same as the Java query's NULLS LAST.
    public Task<List<CdRelease>> FindByArtistIdAndTypesOrderByFirstReleaseDateAsync(Guid artistId, string[] types) =>
        db.CdReleases
            .Where(r => r.ArtistId == artistId && types.Contains(r.ReleaseType))
            .OrderBy(r => r.FirstReleaseDate)
            .ToListAsync();

    public async Task<HashSet<string>> FindTitlesByArtistIdAsync(Guid artistId) =>
        (await db.CdReleases.Where(r => r.ArtistId == artistId).Select(r => r.Title).ToListAsync()).ToHashSet();

    // One SaveChanges = one transaction: either every new release of the batch is stored or none.
    public async Task SaveAllAsync(IReadOnlyCollection<CdRelease> releases)
    {
        if (releases.Count == 0)
        {
            return;
        }
        db.CdReleases.AddRange(releases);
        await db.SaveChangesAsync();
    }
}
