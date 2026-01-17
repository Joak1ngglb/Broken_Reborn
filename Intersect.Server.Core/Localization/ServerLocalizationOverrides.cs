using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Intersect.Server.Localization;

internal static partial class ServerLocalizationOverrides
{
    internal static JObject GetOverrides(string language)
    {
        var overrides = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        AddEnOverrides(overrides);
        AddEsOverrides(overrides);
        AddPtOverrides(overrides);
        AddFrOverrides(overrides);
        AddRuOverrides(overrides);

        return overrides.TryGetValue(language, out var value) ? value : new JObject();
    }

    private static partial void AddEnOverrides(IDictionary<string, JObject> overrides);

    private static partial void AddEsOverrides(IDictionary<string, JObject> overrides);

    private static partial void AddPtOverrides(IDictionary<string, JObject> overrides);

    private static partial void AddFrOverrides(IDictionary<string, JObject> overrides);

    private static partial void AddRuOverrides(IDictionary<string, JObject> overrides);
}
