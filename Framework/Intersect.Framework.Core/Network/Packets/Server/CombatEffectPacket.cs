using MessagePack;

namespace Intersect.Network.Packets.Server;

[MessagePackObject]
public partial class CombatEffectPacket : IntersectPacket
{
    public CombatEffectPacket()
    {
    }

    public CombatEffectPacket(
        Guid targetId,
        float shakeAmount,
        float flashIntensity,
        int flashDurationMs,
        Color flashColor,
        string sound,
        Color entityFlashColor,
        float entityFlashIntensity,
        int entityFlashDurationMs
    )
    {
        TargetId = targetId;
        ShakeAmount = shakeAmount;
        FlashIntensity = flashIntensity;
        FlashDurationMs = flashDurationMs;
        FlashColor = flashColor;
        Sound = sound;
        EntityFlashColor = entityFlashColor;
        EntityFlashIntensity = entityFlashIntensity;
        EntityFlashDurationMs = entityFlashDurationMs;
    }

    [Key(0)]
    public Guid TargetId { get; set; }

    [Key(1)]
    public float ShakeAmount { get; set; }

    [Key(2)]
    public float FlashIntensity { get; set; }

    [Key(3)]
    public int FlashDurationMs { get; set; }

    [Key(4)]
    public Color FlashColor { get; set; } = Color.White;

    [Key(5)]
    public string Sound { get; set; } = string.Empty;

    [Key(6)]
    public Color EntityFlashColor { get; set; } = Color.White;

    [Key(7)]
    public float EntityFlashIntensity { get; set; }

    [Key(8)]
    public int EntityFlashDurationMs { get; set; }
}
