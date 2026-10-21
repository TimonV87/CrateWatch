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
    public Task<List<CdRelease>> FindByArtistIdOrderByFirstReleaseDateAsync(Guid artistId) =>
        db.CdReleases
            .Where(r => r.ArtistId == artistId)
            .OrderBy(r => r.FirstReleaseDate)
            .ToListAsync();

    public Task<bool> ExistsByArtistIdAndTitleAsync(Guid artistId, string title) =>
        db.CdReleases.AnyAsync(r => r.ArtistId == artistId && r.Title == title);

    public async Task<CdRelease> SaveAsync(CdRelease release)
    {
        db.CdReleases.Add(release);
        await db.SaveChangesAsync();
        return release;
    }
}
