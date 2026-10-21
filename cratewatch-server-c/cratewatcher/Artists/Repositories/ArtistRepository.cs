using CrateWatch.Artists.Domain;
using CrateWatch.Db;
using Microsoft.EntityFrameworkCore;

namespace CrateWatch.Artists.Repositories;

public class ArtistRepository(CrateWatchDbContext db)
{
    public Task<Artist?> FindByIdAsync(Guid id) =>
        db.Artists.FirstOrDefaultAsync(a => a.Id == id);

    public Task<Artist?> FindByMusicbrainzIdAsync(string musicbrainzId) =>
        db.Artists.FirstOrDefaultAsync(a => a.MusicbrainzId == musicbrainzId);

    public Task<List<Artist>> FindByIdInAsync(List<Guid> ids) =>
        db.Artists.Where(a => ids.Contains(a.Id)).ToListAsync();

    public async Task<Artist> SaveAsync(Artist artist)
    {
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        return artist;
    }

    public async Task<Artist> UpdateAsync(Artist artist)
    {
        db.Artists.Update(artist);
        await db.SaveChangesAsync();
        return artist;
    }

    public async Task DeleteAsync(Artist artist)
    {
        db.Artists.Remove(artist);
        await db.SaveChangesAsync();
    }
}
