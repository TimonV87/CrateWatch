using CrateWatch.Db;
using CrateWatch.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrateWatch.Users.Repositories;

public class UserRepository(CrateWatchDbContext db)
{
    public Task<User?> FindByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User> SaveAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
