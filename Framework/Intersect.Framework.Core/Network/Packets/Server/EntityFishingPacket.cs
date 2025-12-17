using System;
using Intersect.Enums;
using Intersect.Network;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class EntityFishingPacket : IntersectPacket
{
    public EntityFishingPacket()
    {
    }

    public EntityFishingPacket(Guid id, EntityType type, Guid mapId, bool isFishing, int stage, bool isPressed)
    {
        Id = id;
        Type = type;
        MapId = mapId;
        IsFishing = isFishing;
        Stage = stage;
        IsPressed = isPressed;
    }

    [Key(0)]
    public Guid Id { get; set; }

    [Key(1)]
    public EntityType Type { get; set; }

    [Key(2)]
    public Guid MapId { get; set; }

    [Key(3)]
    public bool IsFishing { get; set; }

    [Key(4)]
    public int Stage { get; set; }

    [Key(5)]
    public bool IsPressed { get; set; }
}
