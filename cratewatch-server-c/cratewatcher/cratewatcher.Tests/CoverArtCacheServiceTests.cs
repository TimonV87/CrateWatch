using System.Net;
using CrateWatch.Caching;
using CrateWatch.Configuration;
using CrateWatch.CoverArt;
using CrateWatch.Releases.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrateWatcher.Tests;

public sealed class CoverArtCacheServiceTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "cw-cover-test-" + Guid.NewGuid());
    private readonly CdRelease release = new()
    {
        Id = Guid.NewGuid(), Title = "T", ReleaseType = "STUDIO_ALBUM", MusicbrainzReleaseGroupId = "rg",
    };

    public void Dispose() => Directory.Delete(dir, recursive: true);

    private string MissingFile => Path.Combine(dir, $"{release.Id}.missing");
    private string ImageFile => Path.Combine(dir, $"{release.Id}.jpg");

    private CoverArtCacheService Create(HttpStatusCode status)
    {
        Directory.CreateDirectory(dir);
        var http = new HttpClient(new StubHandler(status)) { BaseAddress = new Uri("https://caa.test/") };
        return new CoverArtCacheService(
            new CoverArtArchiveClient(http),
            new StoragePaths(dir, dir),
            new ImageFileCache(NullLogger<ImageFileCache>.Instance));
    }

    [Fact]
    public async Task Success_CachesImageAndServesItFromDisk()
    {
        var service = Create(HttpStatusCode.OK);

        Assert.Equal(new byte[] { 1, 2, 3 }, await service.GetCoverArtAsync(release));
        Assert.True(File.Exists(ImageFile));
    }

    [Fact]
    public async Task NotFound_WritesMissingMarker()
    {
        var service = Create(HttpStatusCode.NotFound);

        Assert.Null(await service.GetCoverArtAsync(release));
        Assert.True(File.Exists(MissingFile));
    }

    [Fact]
    public async Task ServerError_DoesNotWriteMissingMarker()
    {
        var service = Create(HttpStatusCode.ServiceUnavailable);

        Assert.Null(await service.GetCoverArtAsync(release));
        Assert.False(File.Exists(MissingFile));
    }

    [Fact]
    public async Task NoReleaseGroupId_ReturnsNullWithoutTouchingDisk()
    {
        var service = Create(HttpStatusCode.OK);
        release.MusicbrainzReleaseGroupId = null;

        Assert.Null(await service.GetCoverArtAsync(release));
        Assert.False(File.Exists(MissingFile));
        Assert.False(File.Exists(ImageFile));
    }

    private sealed class StubHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent([1, 2, 3]) });
    }
}
