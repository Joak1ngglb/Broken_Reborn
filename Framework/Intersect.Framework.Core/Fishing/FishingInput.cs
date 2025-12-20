namespace Intersect.Fishing;

[System.Flags]
public enum FishingInputFlags
{
    None = 0,
    Tap = 1 << 0,
}

public struct FishingInputFrame
{
    public uint TickMs { get; init; }

    public FishingInputFlags Flags { get; init; }
}

public enum FishingSimResult
{
    InProgress,
    Success,
    Failed,
}
