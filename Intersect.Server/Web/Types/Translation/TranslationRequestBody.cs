using Newtonsoft.Json;

namespace Intersect.Server.Web.Types.Translation;

public sealed class TranslationRequestBody
{
    [JsonProperty("text")]
    public string? Text { get; set; }

    [JsonProperty("batch")]
    public Dictionary<string, string>? Batch { get; set; }

    [JsonProperty("targetLanguage")]
    public string? TargetLanguage { get; set; }
}
