using MessagePack;

namespace Intersect.Fishing;

[MessagePackObject]
public sealed class FishingSimStateDto
{
    [Key(0)]
    public float CurrentValue { get; set; }

    [Key(1)]
    public float FishPosition { get; set; }

    [Key(2)]
    public float FishMoveSpeed { get; set; }

    [Key(3)]
    public float RangeSize { get; set; }

    [Key(4)]
    public float TargetRangeSize { get; set; }

    [Key(5)]
    public float PlayerPosition { get; set; }

    [Key(6)]
    public float PullMeter { get; set; }

    [Key(7)]
    public long NextSpeedChangeAtMs { get; set; }

    [Key(8)]
    public long NextRangeChangeAtMs { get; set; }

    [Key(9)]
    public long TickMs { get; set; }

    public static FishingSimStateDto FromState(FishingSimState state)
    {
        return new FishingSimStateDto
        {
            CurrentValue = state.CurrentValue,
            FishPosition = state.FishPosition,
            FishMoveSpeed = state.FishMoveSpeed,
            RangeSize = state.RangeSize,
            TargetRangeSize = state.TargetRangeSize,
            PlayerPosition = state.PlayerPosition,
            PullMeter = state.PullMeter,
            NextSpeedChangeAtMs = state.NextSpeedChangeAtMs,
            NextRangeChangeAtMs = state.NextRangeChangeAtMs,
            TickMs = state.TickMs,
        };
    }

    public FishingSimState ToState()
    {
        return new FishingSimState
        {
            CurrentValue = CurrentValue,
            FishPosition = FishPosition,
            FishMoveSpeed = FishMoveSpeed,
            RangeSize = RangeSize,
            TargetRangeSize = TargetRangeSize,
            PlayerPosition = PlayerPosition,
            PullMeter = PullMeter,
            NextSpeedChangeAtMs = NextSpeedChangeAtMs,
            NextRangeChangeAtMs = NextRangeChangeAtMs,
            TickMs = TickMs,
        };
    }
}
