using Intersect.Network.Packets.Localization;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class EventDialogPacket : IntersectPacket
{
    //Parameterless Constructor for MessagePack
    public EventDialogPacket()
    {
    }

    public EventDialogPacket(
        Guid eventId,
        string prompt,
        string face,
        byte type,
        string[] responses,
        LocalizationRequestEntry? promptLocalizationRequest = null,
        List<LocalizationRequestEntry>? responseLocalizationRequests = null
    )
    {
        EventId = eventId;
        Prompt = prompt;
        Face = face;
        Type = type;
        Responses = responses;
        PromptLocalizationRequest = promptLocalizationRequest;
        ResponseLocalizationRequests = responseLocalizationRequests;
    }

    [Key(0)]
    public Guid EventId { get; set; }

    [Key(1)]
    public string Prompt { get; set; }

    [Key(2)]
    public string Face { get; set; }

    [Key(3)]
    public byte Type { get; set; }

    [Key(4)]
    public string[] Responses { get; set; }

    [Key(5)]
    public LocalizationRequestEntry? PromptLocalizationRequest { get; set; }

    [Key(6)]
    public List<LocalizationRequestEntry>? ResponseLocalizationRequests { get; set; }

}
