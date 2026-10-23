using CrateWatch.Db;
using CrateWatch.Releases.Domain;
using CrateWatch.Shops.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrateWatch.Shops.Repositories;

public class ShopRepository(CrateWatchDbContext db)
{
    public Task<List<Shop>> FindByUserIdAsync(Guid userId) =>
        db.Shops.Where(s => s.UserId == userId).OrderBy(s => s.Name).ToListAsync();

    public Task<Shop?> FindByIdAndUserIdAsync(Guid id, Guid userId) =>
        db.Shops.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

    public Task<int> CountByUserIdAsync(Guid userId) =>
        db.Shops.CountAsync(s => s.UserId == userId);

    public async Task<Shop> SaveAsync(Shop shop)
    {
        db.Shops.Add(shop);
        await db.SaveChangesAsync();
        return shop;
    }

    public async Task DeleteAsync(Shop shop)
    {
        db.Shops.Remove(shop);
        await db.SaveChangesAsync();
    }

    // Only releases that are still on the wishlist.
    public async Task<List<(ShopAvailability Availability, string ShopName, CdRelease Release, string ArtistName)>> FindWishlistAvailabilityAsync(Guid userId)
    {
        var rows = await (
            from a in db.ShopAvailabilities
            join s in db.Shops on a.ShopId equals s.Id
            where s.UserId == userId
            join st in db.UserReleaseStates on a.CdReleaseId equals st.CdReleaseId
            where st.UserId == userId && st.State == "WISHLIST"
            join r in db.CdReleases on a.CdReleaseId equals r.Id
            join ar in db.Artists on r.ArtistId equals ar.Id
            orderby ar.Name, r.FirstReleaseDate, s.Name
            select new { Availability = a, ShopName = s.Name, Release = r, ArtistName = ar.Name }).ToListAsync();
        return rows.Select(x => (x.Availability, x.ShopName, x.Release, x.ArtistName)).ToList();
    }

    // Replaces the stored result of every checked (shop, release) pair; pairs that were not checked keep their old result.
    public async Task SaveScanAsync(
        List<Guid> shopIds, HashSet<(Guid ShopId, Guid ReleaseId)> checkedPairs, List<ShopAvailability> found)
    {
        var existing = await db.ShopAvailabilities.Where(a => shopIds.Contains(a.ShopId)).ToListAsync();
        db.ShopAvailabilities.RemoveRange(existing.Where(a => checkedPairs.Contains((a.ShopId, a.CdReleaseId))));
        db.ShopAvailabilities.AddRange(found);
        await db.SaveChangesAsync();
    }
}
