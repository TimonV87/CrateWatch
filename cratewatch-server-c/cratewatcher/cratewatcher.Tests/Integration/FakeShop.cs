using System.Net;

namespace CrateWatch.Tests.Integration;

/// <summary>
/// A webshop whose search page lists a product only for titles that start with "Wanted", after an optional delay.
/// The app searches for "{artist} {title}", so the title is whatever follows the artist name "Test Artist".
/// </summary>
public sealed class FakeShop : HttpMessageHandler
{
    public int DelayMs { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (DelayMs > 0)
        {
            await Task.Delay(DelayMs, cancellationToken);
        }

        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query)["q"]!;
        var title = query["Test Artist ".Length..];
        var html = title.StartsWith("Wanted")
            ? $"<html><body><div>Test Artist</div><a href=\"/product/1\">{title}</a></body></html>"
            : "<html><body><p>No results</p></body></html>";
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html") };
    }
}
