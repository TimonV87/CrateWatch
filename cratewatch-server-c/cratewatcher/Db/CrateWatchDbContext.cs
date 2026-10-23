using CrateWatch.Artists.Domain;
using CrateWatch.Releases.Domain;
using CrateWatch.Shops.Domain;
using CrateWatch.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrateWatch.Db;

public class CrateWatchDbContext(DbContextOptions<CrateWatchDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<UserArtist> UserArtists => Set<UserArtist>();
    public DbSet<CdRelease> CdReleases => Set<CdRelease>();
    public DbSet<UserReleaseState> UserReleaseStates => Set<UserReleaseState>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopAvailability> ShopAvailabilities => Set<ShopAvailability>();

    // Schema is owned by the SQL migrations (Db/Migration), not by EF. Column names come from
    // the snake_case naming convention registered in Program.cs; ids default to uuidv7() in the DB.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
        });

        modelBuilder.Entity<Artist>(e =>
        {
            e.ToTable("artists");
            e.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
        });

        modelBuilder.Entity<UserArtist>(e =>
        {
            e.ToTable("user_artists");
            e.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
        });

        modelBuilder.Entity<CdRelease>(e =>
        {
            e.ToTable("cd_releases");
            e.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
        });

        modelBuilder.Entity<UserReleaseState>(e =>
        {
            e.ToTable("user_release_states");
            e.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
            e.Property(x => x.State).HasDefaultValue("NONE");
        });

        modelBuilder.Entity<Shop>(e =>
        {
            e.ToTable("shops");
            e.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
        });

        modelBuilder.Entity<ShopAvailability>(e =>
        {
            e.ToTable("shop_availabilities");
            e.Property(x => x.Id).HasDefaultValueSql("uuidv7()");
        });
    }
}
