using CrateWatch.Shops.Services;

namespace CrateWatcher.Tests;

public class ShopJsonParserTests
{
    private static readonly Uri Page = new("https://shop.example/api/search?q=radiohead+ok+computer");

    private const string Example = """
        {"suggestions":[{"name":"Kid A","url":"/p/kid-a"}],
         "data":{"products":[
           {"id":1,"image":"https://cdn.example/1.jpg","title":"OK Computer","artist":{"name":"Radiohead"},"link":"/p/ok-computer-1"},
           {"id":2,"title":"Kid A","artist":{"name":"Radiohead"},"link":"/p/kid-a-2"}]}}
        """;

    [Fact]
    public void LearnsWhereTheExampleAlbumIs()
    {
        var rule = ShopJsonParser.Learn(Example, "Radiohead", "OK Computer");
        Assert.Equal(new ShopJsonParser.Rule("data.products", "link"), rule);
    }

    [Fact]
    public void FindsAnotherAlbumInTheSamePlace()
    {
        var rule = new ShopJsonParser.Rule("data.products", "link");
        Assert.Equal("https://shop.example/p/kid-a-2", ShopJsonParser.FindProductUrl(Example, Page, "Radiohead", "Kid A", rule));
        Assert.Null(ShopJsonParser.FindProductUrl(Example, Page, "Radiohead", "Amnesiac", rule));
    }

    [Fact]
    public void WorksWhenTheAnswerIsAPlainList()
    {
        const string json = """[{"name":"Radiohead - OK Computer","url":"https://shop.example/p/1"}]""";
        var rule = ShopJsonParser.Learn(json, "Radiohead", "OK Computer");
        Assert.Equal(new ShopJsonParser.Rule("", "url"), rule);
        Assert.Equal("https://shop.example/p/1", ShopJsonParser.FindProductUrl(json, Page, "Radiohead", "OK Computer", rule!));
    }

    [Fact]
    public void LearnsNothingWhenTheAlbumIsMissing()
    {
        Assert.Null(ShopJsonParser.Learn(Example, "Radiohead", "Amnesiac"));
    }

    [Theory]
    [InlineData("""{"a":1}""", true)]
    [InlineData("  [1]", true)]
    [InlineData("<html>{}</html>", false)]
    public void LooksLikeJson(string body, bool expected) => Assert.Equal(expected, ShopJsonParser.LooksLikeJson(body));
}
