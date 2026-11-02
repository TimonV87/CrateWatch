using System.Collections.Concurrent;
using System.Text.Json;
using CrateWatch.Releases.Domain;
using CrateWatch.Releases.Repositories;
using CrateWatch.Shops.Domain;
using CrateWatch.Shops.Repositories;

namespace CrateWatch.Shops.Services;

public class ShopService(
    ShopRepository shopRepository,
    UserReleaseStateRepository userReleaseStateRepository,
    ShopScanner scanner,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<ShopService> log)
{
    private const int MaxShops = 10;
    private const int MinDelayMs = 1000;
    private const int MaxDelayMs = 2000;

    // shortcut: in-process, only safe with one backend instance (same as ArtistService.CreateLocks); a second scan by the same user would trip the unique index.
    // Also forgotten on restart, so a scan that was running then is simply gone. Holds each user's latest scan, running or finished.
    private static readonly Dictionary<Guid, ScanProgress> Scans = new();

    // Written by the background scan, read by the status endpoint.
    private sealed class ScanProgress
    {
        private int done;
        public volatile bool Running = true;
        public volatile bool Failed;
        public int Total;
        public volatile ScanResult? Result;

        public void CountDone() => Interlocked.Increment(ref done);

        public ScanStatus ToStatus() => new(Running, Volatile.Read(ref done), Volatile.Read(ref Total), Result, Failed);
    }

    public async Task<List<ShopResponse>> ListAsync(Guid userId) =>
        (await shopRepository.FindByUserIdAsync(userId)).Select(ShopResponse.From).ToList();

    public async Task<ShopResponse> AddAsync(Guid userId, CreateShopRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var template = request.SearchUrlTemplate.Trim();
        if (!IsValidTemplate(template))
        {
            throw new InvalidShopException($"The address must start with http:// or https:// and contain {ShopScanner.QueryPlaceholder}.");
        }
        if (await shopRepository.CountByUserIdAsync(userId) >= MaxShops)
        {
            throw new InvalidShopException($"You can add at most {MaxShops} stores.");
        }

        var shop = new Shop { UserId = userId, Name = name, SearchUrlTemplate = template };
        try
        {
            if (!await scanner.LearnAsync(shop, request.ExampleArtist.Trim(), request.ExampleTitle.Trim(), ct))
            {
                throw new InvalidShopException("The store's answer does not contain the example album. Check the address and the example.");
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            throw new InvalidShopException("Could not read the store's answer for the example: " + e.Message);
        }

        return ShopResponse.From(await shopRepository.SaveAsync(shop));
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var shop = await shopRepository.FindByIdAndUserIdAsync(id, userId);
        if (shop is null)
        {
            return false;
        }

        await shopRepository.DeleteAsync(shop);
        return true;
    }

    // A scan takes minutes (a pause per check, per shop), longer than the proxies in front allow for one request
    // (NPMplus 60 s, Cloudflare 100 s). So it runs in the background and the page polls GetScanStatus.
    public ScanStatus StartScan(Guid userId)
    {
        ScanProgress progress;
        lock (Scans)
        {
            if (Scans.TryGetValue(userId, out var current) && current.Running)
            {
                throw new ScanInProgressException();
            }
            progress = Scans[userId] = new ScanProgress();
        }

        _ = Task.Run(() => RunInBackgroundAsync(userId, progress));
        return progress.ToStatus();
    }

    public static ScanStatus GetScanStatus(Guid userId)
    {
        lock (Scans)
        {
            return Scans.TryGetValue(userId, out var progress) ? progress.ToStatus() : new ScanStatus(false, 0, 0, null, false);
        }
    }

    private async Task RunInBackgroundAsync(Guid userId, ScanProgress progress)
    {
        try
        {
            // The request that started this is long finished, so use a fresh scope (its own DbContext)
            // and the app's shutdown token instead of the request's.
            await using var scope = scopeFactory.CreateAsyncScope();
            progress.Result = await scope.ServiceProvider.GetRequiredService<ShopService>().ScanAsync(userId, progress, lifetime.ApplicationStopping);
        }
        catch (Exception e)
        {
            progress.Failed = true;
            log.LogError(e, "Shop scan failed for user {UserId}", userId);
        }
        finally
        {
            progress.Running = false;
        }
    }

    private async Task<ScanResult> ScanAsync(Guid userId, ScanProgress progress, CancellationToken ct)
    {
        var shops = await shopRepository.FindByUserIdAsync(userId);
        var wishlist = await userReleaseStateRepository.FindReleasesByStateAsync(userId, "WISHLIST");
        progress.Total = shops.Count * wishlist.Count;
        var checks = new ConcurrentBag<ShopCheck>();
        var now = DateTimeOffset.UtcNow;

        // Shops are scanned in parallel; within one shop the requests are sequential, so no shop sees a burst.
        await Parallel.ForEachAsync(shops, new ParallelOptions { MaxDegreeOfParallelism = MaxShops, CancellationToken = ct },
            async (shop, token) =>
            {
                foreach (var check in await ScanShopAsync(shop, wishlist, progress, token))
                {
                    checks.Add(check);
                }
            });

        var checkedPairs = checks.Select(c => (c.ShopId, c.ReleaseId)).ToHashSet();
        var found = checks
            .Where(c => c.Url is not null)
            .Select(c => new ShopAvailability { ShopId = c.ShopId, CdReleaseId = c.ReleaseId, Url = c.Url!, CheckedAt = now })
            .ToList();

        await shopRepository.SaveScanAsync(shops.Select(s => s.Id).ToList(), checkedPairs, found);
        return new ScanResult(checkedPairs.Count, found.Count, shops.Count * wishlist.Count - checkedPairs.Count);
    }

    private async Task<List<ShopCheck>> ScanShopAsync(Shop shop, List<(CdRelease Release, string ArtistName)> wishlist, ScanProgress progress, CancellationToken ct)
    {
        var checks = new List<ShopCheck>();
        foreach (var (index, (release, artistName)) in wishlist.Index())
        {
            if (index > 0)
            {
                await Task.Delay(Random.Shared.Next(MinDelayMs, MaxDelayMs + 1), ct);
            }
            if (await CheckAsync(shop, release, artistName, ct) is { } check)
            {
                checks.Add(check);
            }
            progress.CountDone();
        }
        return checks;
    }

    // Null when the shop could not be read: the pair is left out, so its previous result stays.
    private async Task<ShopCheck?> CheckAsync(Shop shop, CdRelease release, string artistName, CancellationToken ct)
    {
        try
        {
            return new ShopCheck(shop.Id, release.Id, await scanner.FindAsync(shop, artistName, release.Title, ct));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            return null;
        }
    }

    private sealed record ShopCheck(Guid ShopId, Guid ReleaseId, string? Url);

    public async Task<List<AvailableRelease>> AvailabilityAsync(Guid userId)
    {
        var rows = await shopRepository.FindWishlistAvailabilityAsync(userId);
        return rows
            .GroupBy(r => r.Release.Id)
            .Select(g => new AvailableRelease(
                ReleaseResponse.From(g.First().Release, "WISHLIST", g.First().ArtistName),
                g.Select(r => new ShopOffer(r.Availability.ShopId, r.ShopName, r.Availability.Url, r.Availability.CheckedAt)).ToList()))
            .ToList();
    }

    private static bool IsValidTemplate(string template) =>
        template.Contains(ShopScanner.QueryPlaceholder)
        && Uri.TryCreate(template.Replace(ShopScanner.QueryPlaceholder, "x"), UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https";
}
