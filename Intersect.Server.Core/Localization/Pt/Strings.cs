using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Intersect.Server.Localization;

internal static partial class ServerLocalizationOverrides
{
    private static partial void AddPtOverrides(IDictionary<string, JObject> overrides)
    {
        overrides["pt"] = new JObject();
    }
}
