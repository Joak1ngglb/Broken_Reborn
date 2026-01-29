using Intersect.Framework.Core.Localization;
using Intersect.Network.Packets;
using MessagePack;

namespace Intersect.Network.Packets.Localization;

[MessagePackObject]
public sealed partial class TranslationPendingRequestPacket : EditorPacket
{
    public TranslationPendingRequestPacket()
    {
    }

    public TranslationPendingRequestPacket(
        string? entityType,
        TranslationStatus? status,
        string? search,
        string? language,
        int limit,
        int offset
    )
    {
        EntityType = entityType;
        Status = status;
        Search = search;
        Language = language;
        Limit = limit;
        Offset = offset;
    }

    [Key(0)]
    public string? EntityType { get; set; }

    [Key(1)]
    public TranslationStatus? Status { get; set; }

    [Key(2)]
    public string? Search { get; set; }

    [Key(3)]
    public string? Language { get; set; }

    [Key(4)]
    public int Limit { get; set; }

    [Key(5)]
    public int Offset { get; set; }
}
