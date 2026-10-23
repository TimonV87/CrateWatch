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
    ShopScanner scanner)
{
    public const int MaxShops = 10;
    private const int ParallelRequests = 4;

    // shortcut: in-process, only safe with one backend instance (same as ArtistService.CreateLocks); a second scan by the same user would trip the unique index.
    private static readonly ConcurrentDictionary<Guid, byte> RunningScans = new();

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

    // shortcut: runs inside the request, fine for a few hundred checks (shops x wishlist); move to a background job beyond that.
    public async Task<ScanResult> ScanAsync(Guid userId, CancellationToken ct)
    {
        if (!RunningScans.TryAdd(userId, 0))
        {
            throw new ScanInProgressException();
        }

        try
        {
            var shops = await shopRepository.FindByUserIdAsync(userId);
            var wishlist = await userReleaseStateRepository.FindReleasesByStateAsync(userId, "WISHLIST");
            var pairs = shops.SelectMany(s => wishlist.Select(w => (Shop: s, w.Release, w.ArtistName))).ToList();

            var checkedPairs = new ConcurrentBag<(Guid ShopId, Guid ReleaseId)>();
            var found = new ConcurrentBag<ShopAvailability>();
            var now = DateTimeOffset.UtcNow;

            await Parallel.ForEachAsync(pairs, new ParallelOptions { MaxDegreeOfParallelism = ParallelRequests, CancellationToken = ct },
                async (pair, token) =>
                {
                    try
                    {
                        var url = await scanner.FindAsync(pair.Shop, pair.ArtistName, pair.Release.Title, token);
                        checkedPairs.Add((pair.Shop.Id, pair.Release.Id));
                        if (url is not null)
                        {
                            found.Add(new ShopAvailability { ShopId = pair.Shop.Id, CdReleaseId = pair.Release.Id, Url = url, CheckedAt = now });
                        }
                    }
                    catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException && !token.IsCancellationRequested)
                    {
                        // Left out of checkedPairs: the previous result for this pair stays.
                    }
                });

            await shopRepository.SaveScanAsync(shops.Select(s => s.Id).ToList(), checkedPairs.ToHashSet(), found.ToList());
            return new ScanResult(checkedPairs.Count, found.Count, pairs.Count - checkedPairs.Count);
        }
        finally
        {
            RunningScans.TryRemove(userId, out _);
        }
    }

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
