using System.Text.Json;

namespace CrateWatch.Caching;

/// <summary>
/// Disk cache for images keyed by id: <c>{id}.jpg</c> for a hit, <c>{id}.missing</c> for "the source has no image".
/// </summary>
public class ImageFileCache(ILogger<ImageFileCache> log)
{
    /// <param name="fetch">
    /// Returns the image bytes, or null when the source cleanly says there is no image (a permanent marker is written).
    /// Throw for transient failures: nothing is cached and the next call retries.
    /// </param>
    public async Task<byte[]?> GetAsync(string directory, Guid id, Func<Task<byte[]?>> fetch)
    {
        var imagePath = Path.Combine(directory, $"{id}.jpg");
        var missingPath = Path.Combine(directory, $"{id}.missing");

        try
        {
            if (File.Exists(imagePath))
            {
                return await File.ReadAllBytesAsync(imagePath);
            }
            if (File.Exists(missingPath))
            {
                return null;
            }

            var bytes = await fetch();
            Directory.CreateDirectory(directory);
            if (bytes is null)
            {
                await WriteAtomicAsync(missingPath, []);
                return null;
            }
            await WriteAtomicAsync(imagePath, bytes);
            return bytes;
        }
        catch (Exception e) when (e is IOException or HttpRequestException or TaskCanceledException or UriFormatException or JsonException)
        {
            log.LogError(e, "Image cache failed for {Id}", id);
            return null;
        }
    }

    // Write to a temp file and rename, so a concurrent reader never sees a half-written image.
    private static async Task WriteAtomicAsync(string path, byte[] bytes)
    {
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(tempPath, bytes);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            File.Delete(tempPath);
            throw;
        }
    }
}
