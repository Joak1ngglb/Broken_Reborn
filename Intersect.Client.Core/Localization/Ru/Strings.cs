using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Localization;

internal static partial class ClientLocalizationOverrides
{
    private static partial void AddRuOverrides(IDictionary<string, JObject> overrides)
    {
        overrides["ru"] = new JObject
        {
            ["Internals"] = new JObject
            {
                ["Alignment"] = "Выравнивание",
            }
        };
    }
}
