using CrateWatch.Db;
using CrateWatch.Releases.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrateWatch.Releases.Repositories;

public class UserReleaseStateRepository(CrateWatchDbContext db)
{
    public Task<List<UserReleaseState>> FindByUserIdAndCdReleaseIdInAsync(Guid userId, List<Guid> cdReleaseIds) =>
        db.UserReleaseStates
            .Where(s => s.UserId == userId && cdReleaseIds.Contains(s.CdReleaseId))
            .ToListAsync();

    public async Task<List<(CdRelease Release, string ArtistName)>> FindReleasesByStateAsync(Guid userId, string state)
    {
        var rows = await (
            from s in db.UserReleaseStates
            where s.UserId == userId && s.State == state
            join r in db.CdReleases on s.CdReleaseId equals r.Id
            join a in db.Artists on r.ArtistId equals a.Id
            orderby a.Name, r.FirstReleaseDate
            select new { Release = r, ArtistName = a.Name }).ToListAsync();
        return rows.Select(x => (x.Release, x.ArtistName)).ToList();
    }

    // Single-statement upsert: atomic, so concurrent clicks cannot trip the (user_id, cd_release_id) unique index.
    public Task<int> SetAsync(Guid userId, Guid cdReleaseId, string state) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO user_release_states (user_id, cd_release_id, state)
            VALUES ({userId}, {cdReleaseId}, {state})
            ON CONFLICT (user_id, cd_release_id) DO UPDATE SET state = EXCLUDED.state
            """);
}
