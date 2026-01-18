using Newtonsoft.Json.Linq;

namespace Intersect.Localization;

public readonly record struct SupportedLanguage(string Code, string Label);

public static class SupportedLanguages
{
    public static SupportedLanguage[] All { get; } =
    [
        new SupportedLanguage("en", "English"),
        new SupportedLanguage("es", "Español"),
        new SupportedLanguage("pt", "Português"),
        new SupportedLanguage("fr", "Français"),
        new SupportedLanguage("ru", "Русский"),
    ];
}

public static class LocalizationJsonMerger
{
    public static JObject MergeWithOverrides(JObject baseJson, JObject overrideJson)
    {
        var merged = new JObject();
        foreach (var baseProperty in baseJson.Properties())
        {
            if (overrideJson.TryGetValue(baseProperty.Name, out var overrideToken))
            {
                merged[baseProperty.Name] = MergeToken(baseProperty.Value, overrideToken);
            }
            else
            {
                merged[baseProperty.Name] = baseProperty.Value.DeepClone();
            }
        }

        foreach (var overrideProperty in overrideJson.Properties())
        {
            if (!merged.ContainsKey(overrideProperty.Name))
            {
                merged[overrideProperty.Name] = overrideProperty.Value.DeepClone();
            }
        }

        return merged;
    }

    private static JToken MergeToken(JToken baseToken, JToken overrideToken)
    {
        if (overrideToken.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return baseToken.DeepClone();
        }

        if (baseToken.Type == JTokenType.Object && overrideToken.Type == JTokenType.Object)
        {
            return MergeWithOverrides((JObject)baseToken, (JObject)overrideToken);
        }

        if (overrideToken.Type == JTokenType.String)
        {
            var overrideValue = overrideToken.Value<string>();
            if (!string.IsNullOrWhiteSpace(overrideValue))
            {
                return overrideToken.DeepClone();
            }

            return baseToken.DeepClone();
        }

        return overrideToken.DeepClone();
    }
}
