using CrateWatch.Artists.Domain;
using CrateWatch.Db;
using Microsoft.EntityFrameworkCore;

namespace CrateWatch.Artists.Repositories;

public class UserArtistRepository(CrateWatchDbContext db)
{
    public Task<List<UserArtist>> FindByUserIdAsync(Guid userId) =>
        db.UserArtists.Where(ua => ua.UserId == userId).ToListAsync();

    public Task<bool> ExistsByUserIdAndArtistIdAsync(Guid userId, Guid artistId) =>
        db.UserArtists.AnyAsync(ua => ua.UserId == userId && ua.ArtistId == artistId);

    public async Task<UserArtist> SaveAsync(UserArtist userArtist)
    {
        db.UserArtists.Add(userArtist);
        await db.SaveChangesAsync();
        return userArtist;
    }
}
