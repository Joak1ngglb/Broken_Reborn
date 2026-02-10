using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class FlashScreenPacket : IntersectPacket
{
    public FlashScreenPacket()
    {
    }

    public FlashScreenPacket(float flashIntensity, int flashDurationMs, Color flashColor)
    {
        FlashIntensity = flashIntensity;
        FlashDurationMs = flashDurationMs;
        FlashColor = flashColor;
    }

    [Key(0)]
    public float FlashIntensity { get; set; }

    [Key(1)]
    public int FlashDurationMs { get; set; }

    [Key(2)]
    public Color FlashColor { get; set; } = Color.White;
}
