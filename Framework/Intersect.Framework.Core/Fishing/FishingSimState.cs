namespace Intersect.Fishing;

public sealed class FishingSimState
{
    public float CurrentValue { get; set; }

    public float FishPosition { get; set; }

    public float FishMoveSpeed { get; set; }

    public float RangeSize { get; set; }

    public float TargetRangeSize { get; set; }

    public float PlayerPosition { get; set; }

    public float PullMeter { get; set; }

    public long NextSpeedChangeAtMs { get; set; }

    public long NextRangeChangeAtMs { get; set; }

    public long TickMs { get; set; }
}
