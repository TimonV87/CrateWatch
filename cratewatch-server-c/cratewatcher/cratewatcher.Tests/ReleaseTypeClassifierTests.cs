using CrateWatch.Releases.Services;

namespace CrateWatcher.Tests;

public class ReleaseTypeClassifierTests
{

    [Theory]
    [InlineData("Album", null, "STUDIO_ALBUM")]
    [InlineData("Album", new string[0], "STUDIO_ALBUM")]
    [InlineData("Album", new[] { "Compilation" }, "COMPILATION")]
    [InlineData("Album", new[] { "Live" }, "LIVE")]
    [InlineData("Album", new[] { "Soundtrack" }, "SOUNDTRACK")]
    [InlineData("Album", new[] { "Compilation", "Live" }, "COMPILATION")]
    [InlineData("Album", new[] { "Remix" }, "OTHER")]
    [InlineData("EP", null, "EP")]
    [InlineData("Single", null, "SINGLE")]
    [InlineData("Broadcast", null, "OTHER")]
    [InlineData(null, null, "OTHER")]
    public void Classifies(string? primary, string[]? secondary, string expected)
    {
        Assert.Equal(expected, ReleaseTypeClassifier.Classify(primary, secondary));
    }
}
