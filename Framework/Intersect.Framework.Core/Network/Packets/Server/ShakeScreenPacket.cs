using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class ShakeScreenPacket : IntersectPacket
{
    public ShakeScreenPacket()
    {
    }

    public ShakeScreenPacket(float shakeAmount, int durationMs)
    {
        ShakeAmount = shakeAmount;
        DurationMs = durationMs;
    }

    [Key(0)]
    public float ShakeAmount { get; set; }

    [Key(1)]
    public int DurationMs { get; set; }
}
