using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Localization;

internal static partial class ClientLocalizationOverrides
{
    private static partial void AddPtOverrides(IDictionary<string, JObject> overrides)
    {
        overrides["pt"] = new JObject();
    }
}
