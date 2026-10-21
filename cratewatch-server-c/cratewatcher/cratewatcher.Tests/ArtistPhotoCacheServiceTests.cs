using System.Net;
using CrateWatch.Artists.Domain;
using CrateWatch.Caching;
using CrateWatch.Configuration;
using CrateWatch.TheAudioDb;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrateWatch.Tests;

public sealed class ArtistPhotoCacheServiceTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "cw-photo-test-" + Guid.NewGuid());
    private readonly Artist artist = new() { Id = Guid.NewGuid(), Name = "A", MusicbrainzId = "mbid" };

    public void Dispose() => Directory.Delete(dir, recursive: true);

    private string MissingFile => Path.Combine(dir, $"{artist.Id}.missing");
    private string ImageFile => Path.Combine(dir, $"{artist.Id}.jpg");

    private ArtistPhotoCacheService Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        Directory.CreateDirectory(dir);
        var http = new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("https://audiodb.test/") };
        return new ArtistPhotoCacheService(
            new TheAudioDbClient(http),
            new StubFactory(http),
            new StoragePaths(dir, dir),
            new ImageFileCache(NullLogger<ImageFileCache>.Instance));
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task LookupServerError_DoesNotWriteMissingMarker()
    {
        var service = Create(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        Assert.Null(await service.GetPhotoAsync(artist));
        Assert.False(File.Exists(MissingFile));
    }

    [Fact]
    public async Task NoThumb_WritesMissingMarker()
    {
        var service = Create(_ => Json("""{"artists":null}"""));

        Assert.Null(await service.GetPhotoAsync(artist));
        Assert.True(File.Exists(MissingFile));
    }

    [Fact]
    public async Task ImageServerError_DoesNotWriteMissingMarker()
    {
        var service = Create(r => r.RequestUri!.Host == "audiodb.test"
            ? Json("""{"artists":[{"strArtistThumb":"https://img.test/a.jpg"}]}""")
            : new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        Assert.Null(await service.GetPhotoAsync(artist));
        Assert.False(File.Exists(MissingFile));
    }

    [Fact]
    public async Task Image404_WritesMissingMarker()
    {
        var service = Create(r => r.RequestUri!.Host == "audiodb.test"
            ? Json("""{"artists":[{"strArtistThumb":"https://img.test/a.jpg"}]}""")
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.Null(await service.GetPhotoAsync(artist));
        Assert.True(File.Exists(MissingFile));
    }

    [Fact]
    public async Task Success_CachesImage()
    {
        var service = Create(r => r.RequestUri!.Host == "audiodb.test"
            ? Json("""{"artists":[{"strArtistThumb":"https://img.test/a.jpg"}]}""")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });

        Assert.Equal(new byte[] { 1, 2, 3 }, await service.GetPhotoAsync(artist));
        Assert.True(File.Exists(ImageFile));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class StubFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
