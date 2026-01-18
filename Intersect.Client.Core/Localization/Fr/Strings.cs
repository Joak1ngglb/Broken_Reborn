using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Localization;

internal static partial class ClientLocalizationOverrides
{
    private static partial void AddFrOverrides(IDictionary<string, JObject> overrides)
    {
        overrides["fr"] = new JObject();
    }
}
