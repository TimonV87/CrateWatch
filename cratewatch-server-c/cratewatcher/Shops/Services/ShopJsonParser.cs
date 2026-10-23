using System.Text.Json;
using System.Text.RegularExpressions;

namespace CrateWatch.Shops.Services;

// For shops whose search answers with JSON. Learn finds the example album once, FindProductUrl then looks in the same place.
public static partial class ShopJsonParser
{
    // ItemsPath: property names from the root down to the product objects ("data.products"; arrays do not count, "" = the root).
    public record Rule(string ItemsPath, string UrlField);

    public static bool LooksLikeJson(string body) => body.AsSpan().TrimStart() is ['{' or '[', ..];

    // The deepest object that holds both the title and the artist and has a link of its own. Null when the example is not in the JSON.
    public static Rule? Learn(string json, string artist, string title)
    {
        using var doc = JsonDocument.Parse(json);
        return Learn(doc.RootElement, "", Wanted(artist, title));
    }

    public static string? FindProductUrl(string json, Uri pageUrl, string artist, string title, Rule rule)
    {
        using var doc = JsonDocument.Parse(json);
        var wanted = Wanted(artist, title);
        foreach (var item in Items(doc.RootElement, "", rule.ItemsPath))
        {
            if (Matches(item, wanted)
                && item.TryGetProperty(rule.UrlField, out var link)
                && link.ValueKind == JsonValueKind.String
                && Uri.TryCreate(pageUrl, link.GetString(), out var product)
                && product.Scheme is "http" or "https"
                && product.AbsoluteUri.Length <= 2000)
            {
                return product.AbsoluteUri;
            }
        }
        return null;
    }

    private static Rule? Learn(JsonElement e, string path, (string Artist, string Title) wanted)
    {
        if (e.ValueKind == JsonValueKind.Array)
        {
            return e.EnumerateArray().Select(x => Learn(x, path, wanted)).FirstOrDefault(r => r is not null);
        }
        if (e.ValueKind != JsonValueKind.Object || !Matches(e, wanted))
        {
            return null;
        }

        foreach (var p in e.EnumerateObject())
        {
            if (Learn(p.Value, path.Length == 0 ? p.Name : $"{path}.{p.Name}", wanted) is { } deeper)
            {
                return deeper;
            }
        }

        var links = e.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.String && IsLink(p.Value.GetString()!))
            .Select(p => p.Name).ToList();
        var field = links.FirstOrDefault(n => LinkNameRegex().IsMatch(n)) ?? links.FirstOrDefault();
        return field is null ? null : new Rule(path, field);
    }

    private static IEnumerable<JsonElement> Items(JsonElement e, string path, string target)
    {
        if (e.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in e.EnumerateArray().SelectMany(x => Items(x, path, target)))
            {
                yield return item;
            }
        }
        else if (e.ValueKind == JsonValueKind.Object)
        {
            if (path == target)
            {
                yield return e;
            }
            else if (path.Length == 0 || target.StartsWith(path + "."))
            {
                foreach (var item in e.EnumerateObject().SelectMany(p => Items(p.Value, path.Length == 0 ? p.Name : $"{path}.{p.Name}", target)))
                {
                    yield return item;
                }
            }
        }
    }

    private static (string Artist, string Title) Wanted(string artist, string title) =>
        (ShopPageParser.Normalize(artist), ShopPageParser.Normalize(title));

    // Title and artist may sit in different fields (or nested objects) of the same product.
    private static bool Matches(JsonElement e, (string Artist, string Title) wanted)
    {
        var text = ShopPageParser.Normalize(string.Join(" ", Strings(e)));
        return wanted.Title.Trim().Length > 0 && wanted.Artist.Trim().Length > 0
               && text.Contains(wanted.Title) && text.Contains(wanted.Artist);
    }

    private static IEnumerable<string> Strings(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString()!],
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Strings),
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Strings(p.Value)),
        _ => [],
    };

    private static bool IsLink(string value) =>
        (value.StartsWith("http://") || value.StartsWith("https://") || value.StartsWith('/'))
        && !ImageRegex().IsMatch(value);

    [GeneratedRegex("url|link|href", RegexOptions.IgnoreCase)]
    private static partial Regex LinkNameRegex();

    [GeneratedRegex(@"\.(jpe?g|png|gif|webp|svg)(\?|$)", RegexOptions.IgnoreCase)]
    private static partial Regex ImageRegex();
}
