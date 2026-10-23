using CrateWatch.Shops.Domain;

namespace CrateWatch.Shops.Services;

public class ShopScanner(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "shop-scan";
    public const string QueryPlaceholder = "{query}";

    // Returns a product URL, or null when the shop's search page shows no such album. Throws when the page cannot be fetched.
    public async Task<string?> FindAsync(Shop shop, string artist, string title, CancellationToken ct)
    {
        var (body, url) = await FetchAsync(shop.SearchUrlTemplate, artist, title, ct);
        if (!ShopJsonParser.LooksLikeJson(body))
        {
            return ShopPageParser.FindProductUrl(body, url, artist, title);
        }

        return shop is { JsonItemsPath: { } path, JsonUrlField: { } field }
            ? ShopJsonParser.FindProductUrl(body, url, artist, title, new ShopJsonParser.Rule(path, field))
            : null;
    }

    // Searches the shop for a known album and remembers on the shop where in the answer it was found.
    // Returns false when the answer does not contain the album. Throws when the page cannot be fetched or parsed.
    public async Task<bool> LearnAsync(Shop shop, string artist, string title, CancellationToken ct)
    {
        var (body, url) = await FetchAsync(shop.SearchUrlTemplate, artist, title, ct);
        if (!ShopJsonParser.LooksLikeJson(body))
        {
            shop.JsonItemsPath = shop.JsonUrlField = null;
            return ShopPageParser.FindProductUrl(body, url, artist, title) is not null;
        }

        var rule = ShopJsonParser.Learn(body, artist, title);
        shop.JsonItemsPath = rule?.ItemsPath;
        shop.JsonUrlField = rule?.UrlField;
        return rule is not null;
    }

    private async Task<(string Body, Uri Url)> FetchAsync(string template, string artist, string title, CancellationToken ct)
    {
        var query = Uri.EscapeDataString($"{artist} {title}");
        var url = new Uri(template.Replace(QueryPlaceholder, query));

        using var response = await httpClientFactory.CreateClient(HttpClientName).GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync(ct), response.RequestMessage?.RequestUri ?? url);
    }
}
