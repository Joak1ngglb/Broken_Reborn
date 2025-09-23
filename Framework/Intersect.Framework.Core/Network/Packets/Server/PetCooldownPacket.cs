using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public sealed class PetCooldownPacket : IntersectPacket
{
    public PetCooldownPacket()
    {
    }

    public PetCooldownPacket(long nextInvokeAtMs)
    {
        NextInvokeAtMs = nextInvokeAtMs;
    }

    [Key(0)]
    public long NextInvokeAtMs { get; set; }
}
