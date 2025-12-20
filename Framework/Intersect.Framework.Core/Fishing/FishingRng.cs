using System;

namespace Intersect.Fishing;

public struct FishingRng
{
    public FishingRng(uint seed)
    {
        State = seed;
    }

    public uint State { get; private set; }

    public uint NextUInt()
    {
        unchecked
        {
            State = (State * 1664525u) + 1013904223u;
            return State;
        }
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive must be greater than minInclusive.");
        }

        var range = (uint)(maxExclusive - minInclusive);
        var value = NextUInt();

        return (int)(value % range) + minInclusive;
    }

    public float NextFloat01()
    {
        return NextUInt() / (float)uint.MaxValue;
    }
}
