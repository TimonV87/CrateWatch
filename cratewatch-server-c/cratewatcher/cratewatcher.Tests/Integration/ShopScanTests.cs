using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CrateWatch.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ShopScanTests(ApiFixture api)
{
    // A user with one shop and two wishlist albums; only the one titled "Wanted ..." is in the fake shop.
    private async Task<(HttpClient Client, string WantedTitle)> UserWithWishlistAsync()
    {
        var (client, userId, _) = await api.NewUserAsync();
        var artistId = await api.NewArtistAsync();
        await api.TrackAsync(userId, artistId);
        var wantedTitle = $"Wanted {Guid.NewGuid():N}";
        foreach (var releaseId in new[] { await api.NewReleaseAsync(artistId, wantedTitle), await api.NewReleaseAsync(artistId, $"Other {Guid.NewGuid():N}") })
        {
            (await client.PutAsJsonAsync($"/api/v1/releases/{releaseId}/state", new { state = "WISHLIST" })).EnsureSuccessStatusCode();
        }
        await api.NewShopAsync(userId);
        return (client, wantedTitle);
    }

    private static async Task<JsonElement> WaitForScanAsync(HttpClient client)
    {
        for (var i = 0; i < 60; i++)
        {
            var status = await client.GetFromJsonAsync<JsonElement>("/api/v1/shops/scan");
            if (!status.GetProperty("running").GetBoolean())
            {
                return status;
            }
            await Task.Delay(500);
        }
        throw new TimeoutException("The scan did not finish.");
    }

    [Fact]
    public async Task A_slow_scan_does_not_hold_the_request_open_and_finishes_in_the_background()
    {
        api.Shop.DelayMs = 300; // 2 checks, 300 ms each, plus the 1-2 s pause between them: well over 1 s in total
        var (client, wantedTitle) = await UserWithWishlistAsync();

        var timer = Stopwatch.StartNew();
        var started = await client.PostAsync("/api/v1/shops/scan", null);
        timer.Stop();

        Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        Assert.True(timer.Elapsed < TimeSpan.FromMilliseconds(900), $"POST took {timer.Elapsed}; it must return before the scan is done");
        Assert.True((await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("running").GetBoolean());

        var finished = await WaitForScanAsync(client);

        Assert.False(finished.GetProperty("failed").GetBoolean());
        var result = finished.GetProperty("result");
        Assert.Equal(2, result.GetProperty("checked").GetInt32());
        Assert.Equal(1, result.GetProperty("found").GetInt32());
        Assert.Equal(0, result.GetProperty("failed").GetInt32());
        Assert.Equal(2, finished.GetProperty("done").GetInt32());

        var availability = await client.GetFromJsonAsync<JsonElement>("/api/v1/shops/availability");
        Assert.Equal(1, availability.GetArrayLength());
        Assert.Equal(wantedTitle, availability[0].GetProperty("release").GetProperty("title").GetString());
        Assert.Equal("https://shop.test/product/1", availability[0].GetProperty("offers")[0].GetProperty("url").GetString());
    }

    [Fact]
    public async Task A_second_scan_while_one_is_running_is_409_and_other_users_do_not_see_it()
    {
        api.Shop.DelayMs = 300;
        var (client, _) = await UserWithWishlistAsync();
        var (stranger, _, _) = await api.NewUserAsync();

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsync("/api/v1/shops/scan", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/api/v1/shops/scan", null)).StatusCode);

        var strangerStatus = await stranger.GetFromJsonAsync<JsonElement>("/api/v1/shops/scan");
        Assert.False(strangerStatus.GetProperty("running").GetBoolean());
        Assert.Equal(0, strangerStatus.GetProperty("total").GetInt32());

        await WaitForScanAsync(client); // let it finish so no scan outlives the test
    }

    [Fact]
    public async Task A_finished_scan_can_be_started_again()
    {
        api.Shop.DelayMs = 0;
        var (client, _) = await UserWithWishlistAsync();

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsync("/api/v1/shops/scan", null)).StatusCode);
        await WaitForScanAsync(client);

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsync("/api/v1/shops/scan", null)).StatusCode);
        var second = await WaitForScanAsync(client);
        Assert.Equal(1, second.GetProperty("result").GetProperty("found").GetInt32());
    }
}
