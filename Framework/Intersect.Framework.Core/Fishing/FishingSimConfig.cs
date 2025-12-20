namespace Intersect.Fishing;

public sealed class FishingSimConfig
{
    public float BeginValue { get; set; }

    public float PlayerStrength { get; set; }

    public float HookSize { get; set; }

    public float FishInitialPosition { get; set; }

    public float FishBaseSpeed { get; set; }

    public float FishRangeBaseSize { get; set; }

    public float FishRangeChangeSpeed { get; set; }

    public float FishWeight { get; set; }

    public float FishStrengthDrain { get; set; }

    public float FishPushStrength { get; set; }

    public int TimeChangeSpeedMs { get; set; }

    public int TimeChangeRangeMs { get; set; }

    public float Unpredictability { get; set; }
}
