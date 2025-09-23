using System;
using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public sealed class PetTargetPacket : IntersectPacket
{
    public PetTargetPacket()
    {
    }

    public PetTargetPacket(Guid petId, Guid targetId)
    {
        PetId = petId;
        TargetId = targetId;
    }

    [Key(0)]
    public Guid PetId { get; set; }

    [Key(1)]
    public Guid TargetId { get; set; }
}
