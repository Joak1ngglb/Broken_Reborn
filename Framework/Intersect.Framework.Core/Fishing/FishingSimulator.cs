using System;

namespace Intersect.Fishing;

public static class FishingSimulator
{
    public static FishingSimResult Step(
        ref FishingSimState state,
        in FishingSimConfig config,
        in FishingInputFrame input,
        uint deltaMs,
        ref FishingRng rng)
    {
        var dt = deltaMs / 1000f;

        state.TickMs += deltaMs;
        var now = state.TickMs;

        if ((input.Flags & FishingInputFlags.Tap) != 0)
        {
            state.PullMeter += config.PlayerStrength;
        }

        state.PullMeter = Math.Clamp(state.PullMeter - (config.FishWeight * dt), 0f, 1f);

        var hookHalfSize = config.HookSize * 0.5f;
        state.PlayerPosition += (state.PullMeter * config.FishPushStrength - config.FishWeight) * dt;
        state.PlayerPosition = Math.Clamp(state.PlayerPosition, hookHalfSize, 1f - hookHalfSize);

        UpdateFishMovement(ref state, in config, dt, now, ref rng);
        UpdateFishRange(ref state, in config, dt, now, ref rng);

        var halfRange = state.RangeSize * 0.5f;
        state.FishPosition = Math.Clamp(state.FishPosition, halfRange, 1f - halfRange);

        var hookLeft = state.PlayerPosition - hookHalfSize;
        var hookRight = state.PlayerPosition + hookHalfSize;
        var fishLeft = state.FishPosition - halfRange;
        var fishRight = state.FishPosition + halfRange;

        if ((fishLeft < hookRight && hookRight < fishRight) || (fishLeft < hookLeft && hookLeft < fishRight))
        {
            state.CurrentValue += config.PlayerStrength * dt;
        }
        else
        {
            state.CurrentValue -= config.FishStrengthDrain * dt;
        }

        state.CurrentValue = Math.Clamp(state.CurrentValue, 0f, 1f);

        if (state.CurrentValue <= 0f)
        {
            return FishingSimResult.Failed;
        }

        if (state.CurrentValue >= 1f)
        {
            return FishingSimResult.Success;
        }

        return FishingSimResult.InProgress;
    }

    private static void UpdateFishMovement(
        ref FishingSimState state,
        in FishingSimConfig config,
        float dt,
        long now,
        ref FishingRng rng)
    {
        state.FishPosition += state.FishMoveSpeed * dt;

        var halfRange = state.RangeSize * 0.5f;
        var leftRange = state.FishPosition - halfRange;
        var rightRange = state.FishPosition + halfRange;

        if (leftRange <= 0f || rightRange >= 1f)
        {
            state.FishMoveSpeed *= -1f;
        }

        if (now < state.NextSpeedChangeAtMs)
        {
            return;
        }

        var direction = Math.Sign(state.FishMoveSpeed);
        if (direction == 0)
        {
            direction = rng.NextInt(0, 2) == 0 ? -1 : 1;
        }

        var newSpeed = ApplyVariance(config.FishBaseSpeed, config.Unpredictability, ref rng) * direction;

        var flipRoll = rng.NextFloat01();
        if (flipRoll <= config.Unpredictability / 200f)
        {
            newSpeed *= -1f;
        }

        state.FishMoveSpeed = newSpeed;

        var timeChangeSpeed = (int)MathF.Max(1f, ApplyVariance(config.TimeChangeSpeedMs, config.Unpredictability, ref rng));
        state.NextSpeedChangeAtMs = now + timeChangeSpeed;
    }

    private static void UpdateFishRange(
        ref FishingSimState state,
        in FishingSimConfig config,
        float dt,
        long now,
        ref FishingRng rng)
    {
        if (now >= state.NextRangeChangeAtMs)
        {
            state.TargetRangeSize = ApplyVariance(config.FishRangeBaseSize, config.Unpredictability, ref rng);
            var timeChangeRange = (int)MathF.Max(1f, ApplyVariance(config.TimeChangeRangeMs, config.Unpredictability, ref rng));
            state.NextRangeChangeAtMs = now + timeChangeRange;
        }

        if (Math.Abs(state.RangeSize - state.TargetRangeSize) < 0.001f)
        {
            return;
        }

        var rangeSign = Math.Sign(state.TargetRangeSize - state.RangeSize);
        state.RangeSize += config.FishRangeChangeSpeed * dt * rangeSign;

        if (Math.Abs(state.TargetRangeSize - state.RangeSize) < 0.01f)
        {
            state.RangeSize = state.TargetRangeSize;
        }
    }

    private static float ApplyVariance(float baseValue, float unpredictability, ref FishingRng rng)
    {
        if (unpredictability <= 0)
        {
            return baseValue;
        }

        var variance = rng.NextFloat01() * (unpredictability / 100f);
        var direction = rng.NextInt(0, 2) == 0 ? -1f : 1f;

        return baseValue + (baseValue * variance * direction);
    }
}
