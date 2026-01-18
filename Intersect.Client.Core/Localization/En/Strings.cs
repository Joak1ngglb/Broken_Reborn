using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Localization;

internal static partial class ClientLocalizationOverrides
{
    private static partial void AddEnOverrides(IDictionary<string, JObject> overrides)
    {
        overrides["en"] = new JObject();
    }
}
