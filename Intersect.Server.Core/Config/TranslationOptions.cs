using System.IO;

namespace Intersect.Server.Config;

public sealed class TranslationOptions
{
    public string TranslationApiKey { get; set; } = string.Empty;
}

public static class TranslationConfiguration
{
    public static TranslationOptions Current { get; } = new();

    public static void LoadFromResources(string resourceDirectory)
    {
        var apiKeyPath = Path.Combine(resourceDirectory, "localization", "apikey.txt");
        if (!File.Exists(apiKeyPath))
        {
            Current.TranslationApiKey = string.Empty;
            return;
        }

        Current.TranslationApiKey = File.ReadAllText(apiKeyPath).Trim();
    }
}
