using CrateWatch.Caching;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrateWatch.Tests;

public sealed class ImageFileCacheTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "cw-cache-test-" + Guid.NewGuid());
    private readonly ImageFileCache cache = new(NullLogger<ImageFileCache>.Instance);
    private readonly Guid id = Guid.NewGuid();

    public void Dispose()
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Hit_WritesImageAndLeavesNoTempFile()
    {
        var result = await cache.GetAsync(dir, id, () => Task.FromResult<byte[]?>([1, 2, 3]));

        Assert.Equal(new byte[] { 1, 2, 3 }, result);
        Assert.Equal([$"{id}.jpg"], Directory.GetFiles(dir).Select(Path.GetFileName));
    }

    [Fact]
    public async Task Null_WritesOnlyMissingMarker()
    {
        Assert.Null(await cache.GetAsync(dir, id, () => Task.FromResult<byte[]?>(null)));

        Assert.Equal([$"{id}.missing"], Directory.GetFiles(dir).Select(Path.GetFileName));
    }

    [Fact]
    public async Task FetchThrows_CachesNothing()
    {
        Assert.Null(await cache.GetAsync(dir, id, () => throw new HttpRequestException("boom")));

        Assert.False(Directory.Exists(dir) && Directory.GetFiles(dir).Length > 0);
    }

    [Fact]
    public async Task ParallelCalls_AllGetTheFullImage()
    {
        var big = new byte[2_000_000];
        Random.Shared.NextBytes(big);

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await Task.Yield();
            return await cache.GetAsync(dir, id, () => Task.FromResult<byte[]?>(big));
        }));

        Assert.All(results, r => Assert.Equal(big.Length, r!.Length));
        Assert.Equal(big, await File.ReadAllBytesAsync(Path.Combine(dir, $"{id}.jpg")));
        Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
    }
}
