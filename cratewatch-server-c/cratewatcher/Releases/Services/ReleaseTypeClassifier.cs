namespace CrateWatch.Releases.Services;

public static class ReleaseTypeClassifier
{
    public static string Classify(string? primaryType, IReadOnlyCollection<string>? secondaryTypes)
    {
        IReadOnlyCollection<string> secondary = secondaryTypes ?? [];

        // Arms run top to bottom, so a secondary type wins over the primary type.
        return primaryType switch
        {
            _ when secondary.Contains("Compilation") => "COMPILATION",
            _ when secondary.Contains("Live") => "LIVE",
            _ when secondary.Contains("Soundtrack") => "SOUNDTRACK",
            "Album" when secondary.Count == 0 => "STUDIO_ALBUM",
            "EP" => "EP",
            "Single" => "SINGLE",
            _ => "OTHER"
        };
    }
}
