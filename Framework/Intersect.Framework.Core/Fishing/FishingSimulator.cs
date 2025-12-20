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

        const float volumePressEMultiple = 10f;

        if ((input.Flags & FishingInputFlags.Tap) != 0)
        {
            state.PullMeter += config.PlayerStrength * volumePressEMultiple;
            state.PlayerPosition += config.PlayerStrength;
        }

        state.PlayerPosition -= config.FishWeight * dt;
        state.PullMeter -= config.FishWeight * dt * (volumePressEMultiple * 0.5f);

        var hookHalfSize = config.HookSize * 0.5f;
        state.PlayerPosition = Math.Clamp(state.PlayerPosition, hookHalfSize, 1f - hookHalfSize);
        state.PullMeter = Math.Clamp(state.PullMeter, 0f, 1f);

        state.FishPosition += state.FishMoveSpeed * dt;
        var halfRange = state.RangeSize * 0.5f;
        state.FishPosition = Math.Clamp(state.FishPosition, halfRange, 1f - halfRange);

        var leftRange = state.FishPosition - halfRange;
        var rightRange = state.FishPosition + halfRange;
        if (leftRange <= 0f || rightRange >= 1f)
        {
            state.FishMoveSpeed *= -1f;
        }

        if (now >= state.NextSpeedChangeAtMs)
        {
            var direction = Math.Sign(state.FishMoveSpeed);
            if (direction == 0)
            {
                direction = 1;
            }

            state.FishMoveSpeed = ChaosRange(config.FishBaseSpeed, 0f, config.Unpredictability, 1, 2f, ref rng) * direction;

            var flipRoll = rng.NextInt(1, 201);
            if (flipRoll < config.Unpredictability)
            {
                state.FishMoveSpeed *= -1f;
            }

            var timeChangeSpeed = (int)ChaosRange(config.TimeChangeSpeedMs, 0f, config.Unpredictability, rng: ref rng);
            state.NextSpeedChangeAtMs = now + timeChangeSpeed;
        }

        if (now >= state.NextRangeChangeAtMs)
        {
            state.TargetRangeSize = ChaosRange(config.FishRangeBaseSize, 0f, config.Unpredictability, rng: ref rng);
            var timeChangeRange = (int)ChaosRange(config.TimeChangeRangeMs, 0f, config.Unpredictability, rng: ref rng);
            state.NextRangeChangeAtMs = now + timeChangeRange;
        }

        if (!state.RangeSize.Equals(state.TargetRangeSize))
        {
            var rangeSign = Math.Sign(state.TargetRangeSize - state.RangeSize);
            state.RangeSize += config.FishRangeChangeSpeed * dt * rangeSign;
            if (Math.Abs(state.TargetRangeSize - state.RangeSize) < 0.01f)
            {
                state.RangeSize = state.TargetRangeSize;
            }
        }

        halfRange = state.RangeSize * 0.5f;
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

    private static float ChaosRange(
        float defaultValue,
        float min,
        float max,
        int sign = 0,
        float multiply = 1f,
        ref FishingRng rng)
    {
        var minInt = (int)MathF.Floor(min);
        var maxInt = (int)MathF.Ceiling(max);

        var scale = maxInt > minInt ? rng.NextInt(minInt, maxInt) : minInt;

        var direction = Math.Sign(sign);
        var result = 0f;

        switch (direction)
        {
            case > 0:
                result = scale / 100f;
                break;
            case < 0:
                result = -scale / 100f;
                break;
            default:
                var res = rng.NextInt(1, 101);
                result = res > 50 ? scale / 100f : -scale / 100f;
                break;
        }

        if (scale == 0)
        {
            result = 0f;
        }

        return defaultValue + defaultValue * (result * multiply);
    }
}
