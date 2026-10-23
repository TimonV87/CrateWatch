using System.Net;
using CrateWatch.Shops.Services;

namespace CrateWatcher.Tests;

public class ShopPageParserTests
{
    private static readonly Uri Page = new("https://shop.example/search?q=radiohead+ok+computer");

    [Fact]
    public void FindsProductLinkWithArtistNearby()
    {
        const string html = """
            <div class="p"><span class="artist">Radiohead</span>
            <a href="/cd/ok-computer-123" class="t">OK Computer</a></div>
            """;
        Assert.Equal("https://shop.example/cd/ok-computer-123", ShopPageParser.FindProductUrl(html, Page, "Radiohead", "OK Computer"));
    }

    [Fact]
    public void IgnoresAccentsCaseAndEntities()
    {
        const string html = """<a href="/p/1?a=1&amp;b=2">BEYONCÉ &amp; Friends - Lémonade</a>""";
        Assert.Equal("https://shop.example/p/1?a=1&b=2", ShopPageParser.FindProductUrl(html, Page, "Beyonce", "lemonade"));
    }

    [Fact]
    public void ReturnsNullWhenTitleIsNotInALink()
    {
        const string html = """<h1>Results for Radiohead OK Computer</h1><a href="/cd/kid-a">Kid A</a>""";
        Assert.Null(ShopPageParser.FindProductUrl(html, Page, "Radiohead", "OK Computer"));
    }

    [Fact]
    public void ReturnsNullWhenArtistIsNowhereNearTheLink()
    {
        var html = $"""<a href="/cd/ok-computer">OK Computer</a>{new string(' ', 2000)}Radiohead""";
        Assert.Null(ShopPageParser.FindProductUrl(html, Page, "Radiohead", "OK Computer"));
    }

    [Fact]
    public void SkipsLinksBackToTheSearchPageThatRepeatTheQuery()
    {
        const string html = """
            Radiohead <a href="/search?q=radio+head+ok+computer">Did you mean: radio head OK Computer</a>
            Radiohead <a href="/search?q=Radiohead_OK_Computer&amp;create=1">OK Computer (create)</a>
            Radiohead <a href="/search?id=7">OK Computer</a>
            """;
        var page = new Uri("https://shop.example/search?q=radiohead+ok+computer");
        Assert.Equal("https://shop.example/search?id=7", ShopPageParser.FindProductUrl(html, page, "Radiohead", "OK Computer"));
    }

    [Fact]
    public void MatchesWholeWordsOnly()
    {
        const string html = """Radiohead <a href="/cd/x">Viva la Vida</a>""";
        Assert.Null(ShopPageParser.FindProductUrl(html, Page, "Radiohead", "Via"));
    }

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.20.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("::1", false)]
    [InlineData("::ffff:10.0.0.1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("2606:4700:4700::1111", true)]
    public void IsPublicAddress(string address, bool expected)
    {
        Assert.Equal(expected, ShopHttpHandler.IsPublic(IPAddress.Parse(address)));
    }
}
