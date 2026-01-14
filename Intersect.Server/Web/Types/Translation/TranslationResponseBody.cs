using Newtonsoft.Json;

namespace Intersect.Server.Web.Types.Translation;

public sealed class TranslationResponseBody
{
    [JsonProperty("translation")]
    public string? Translation { get; set; }

    [JsonProperty("translations")]
    public Dictionary<string, string>? Translations { get; set; }
}
