namespace CrateWatch.Releases.Services;

public static class ReleaseTypeClassifier
{
    public static string Classify(string? primaryType, IReadOnlyCollection<string>? secondaryTypes)
    {
        IReadOnlyCollection<string> secondary = secondaryTypes ?? [];

        if (secondary.Contains("Compilation"))
        {
            return "COMPILATION";
        }
        if (secondary.Contains("Live"))
        {
            return "LIVE";
        }
        if (secondary.Contains("Soundtrack"))
        {
            return "SOUNDTRACK";
        }

        return primaryType switch
        {
            "Album" when secondary.Count == 0 => "STUDIO_ALBUM",
            "EP" => "EP",
            "Single" => "SINGLE",
            _ => "OTHER"
        };
    }
}
