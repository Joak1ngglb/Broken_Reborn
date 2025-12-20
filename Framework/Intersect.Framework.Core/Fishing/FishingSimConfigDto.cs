using MessagePack;

namespace Intersect.Fishing;

[MessagePackObject]
public sealed class FishingSimConfigDto
{
    [Key(0)]
    public float BeginValue { get; set; }

    [Key(1)]
    public float PlayerStrength { get; set; }

    [Key(2)]
    public float HookSize { get; set; }

    [Key(3)]
    public float FishInitialPosition { get; set; }

    [Key(4)]
    public float FishBaseSpeed { get; set; }

    [Key(5)]
    public float FishRangeBaseSize { get; set; }

    [Key(6)]
    public float FishRangeChangeSpeed { get; set; }

    [Key(7)]
    public float FishWeight { get; set; }

    [Key(8)]
    public float FishStrengthDrain { get; set; }

    [Key(9)]
    public float FishPushStrength { get; set; }

    [Key(10)]
    public int TimeChangeSpeedMs { get; set; }

    [Key(11)]
    public int TimeChangeRangeMs { get; set; }

    [Key(12)]
    public float Unpredictability { get; set; }

    public static FishingSimConfigDto FromConfig(FishingSimConfig config)
    {
        return new FishingSimConfigDto
        {
            BeginValue = config.BeginValue,
            PlayerStrength = config.PlayerStrength,
            HookSize = config.HookSize,
            FishInitialPosition = config.FishInitialPosition,
            FishBaseSpeed = config.FishBaseSpeed,
            FishRangeBaseSize = config.FishRangeBaseSize,
            FishRangeChangeSpeed = config.FishRangeChangeSpeed,
            FishWeight = config.FishWeight,
            FishStrengthDrain = config.FishStrengthDrain,
            FishPushStrength = config.FishPushStrength,
            TimeChangeSpeedMs = config.TimeChangeSpeedMs,
            TimeChangeRangeMs = config.TimeChangeRangeMs,
            Unpredictability = config.Unpredictability,
        };
    }

    public FishingSimConfig ToConfig()
    {
        return new FishingSimConfig
        {
            BeginValue = BeginValue,
            PlayerStrength = PlayerStrength,
            HookSize = HookSize,
            FishInitialPosition = FishInitialPosition,
            FishBaseSpeed = FishBaseSpeed,
            FishRangeBaseSize = FishRangeBaseSize,
            FishRangeChangeSpeed = FishRangeChangeSpeed,
            FishWeight = FishWeight,
            FishStrengthDrain = FishStrengthDrain,
            FishPushStrength = FishPushStrength,
            TimeChangeSpeedMs = TimeChangeSpeedMs,
            TimeChangeRangeMs = TimeChangeRangeMs,
            Unpredictability = Unpredictability,
        };
    }
}
