using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CrateWatch.Shops.Services;

public static partial class ShopPageParser
{
    // shortcut: finds a product by link text in the served HTML, so shops that render results with JavaScript are not found; upgrade to a per-shop parser if that matters.
    public static string? FindProductUrl(string html, Uri pageUrl, string artist, string title)
    {
        var wantedTitle = Normalize(title);
        var wantedArtist = Normalize(artist);
        if (wantedTitle.Trim().Length == 0 || wantedArtist.Trim().Length == 0)
        {
            return null;
        }

        foreach (Match link in AnchorRegex().Matches(html))
        {
            if (!Normalize(link.Groups[2].Value + " " + link.Groups[3].Value).Contains(wantedTitle))
            {
                continue;
            }

            // The artist is often in a neighbouring element, so look a little around the link.
            var from = Math.Max(0, link.Index - 300);
            var around = html.Substring(from, Math.Min(html.Length - from, link.Length + 600));
            if (!Normalize(around).Contains(wantedArtist))
            {
                continue;
            }

            var href = WebUtility.HtmlDecode(link.Groups[1].Value).Trim();
            if (Uri.TryCreate(pageUrl, href, out var product)
                && product.Scheme is "http" or "https"
                && !IsSearchEcho(product, pageUrl, wantedTitle)
                && product.AbsoluteUri.Length <= 2000)
            {
                return product.AbsoluteUri;
            }
        }

        return null;
    }

    // "Did you mean" and "create this page" links go back to the search page with the query in them; they are not products.
    private static bool IsSearchEcho(Uri link, Uri pageUrl, string wantedTitle) =>
        link.Host == pageUrl.Host
        && link.AbsolutePath == pageUrl.AbsolutePath
        && Normalize(Uri.UnescapeDataString(link.Query)).Contains(wantedTitle);

    // Lower case, no accents, letters and digits only, single spaces, one space at each end so Contains matches whole words.
    internal static string Normalize(string text)
    {
        text = TagRegex().Replace(WebUtility.HtmlDecode(text), " ").Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(" ");
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && sb[^1] != ' ')
            {
                sb.Append(' ');
            }
        }
        return sb.ToString().TrimEnd() + " ";
    }

    // Groups: 1 = href, 2 = the other attributes of the tag (title="..."), 3 = the link content.
    [GeneratedRegex("""<a\s+[^>]*?href\s*=\s*["']([^"']+)["']([^>]*)>(.*?)</a>""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AnchorRegex();

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex TagRegex();
}
