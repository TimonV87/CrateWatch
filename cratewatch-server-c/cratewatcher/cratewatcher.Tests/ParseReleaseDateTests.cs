using CrateWatch.Artists.Services;

namespace CrateWatcher.Tests;

public class ParseReleaseDateTests
{
    [Theory]
    [InlineData("1997-05-21", 1997, 5, 21)]
    [InlineData("1997-05", 1997, 5, 1)]
    [InlineData("1997", 1997, 1, 1)]
    public void ParsesVariablePrecision(string raw, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), ArtistImportService.ParseReleaseDate(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("abcd")]
    [InlineData("1997-13")]
    [InlineData("1997-02-30")]
    [InlineData("1997-xx-01")]
    public void ReturnsNullForUnparseable(string? raw)
    {
        Assert.Null(ArtistImportService.ParseReleaseDate(raw));
    }
}
