using MessagePack;

namespace Intersect.Network.Packets.Localization;

[MessagePackObject]
public sealed partial class LocalizationRequestEntry
{
    public LocalizationRequestEntry()
    {
    }

    public LocalizationRequestEntry(string entityType, string entityId, string field)
    {
        EntityType = entityType;
        EntityId = entityId;
        Field = field;
    }

    [Key(0)]
    public string EntityType { get; set; }

    [Key(1)]
    public string EntityId { get; set; }

    [Key(2)]
    public string Field { get; set; }
}
