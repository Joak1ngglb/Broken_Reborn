using Intersect.Network.Packets.Localization;

namespace Intersect.Client.Entities.Events;

public partial class Dialog
{
    public Guid EventId;

    public string? Face;

    public string[] Options = [];

    public LocalizationRequestEntry? PromptLocalizationRequest;

    public LocalizationRequestEntry[] OptionLocalizationRequests = [];

    public string? Prompt;

    public string? PromptDefault;

    public string[] OptionDefaults = [];

    public bool ResponseSent;
}
