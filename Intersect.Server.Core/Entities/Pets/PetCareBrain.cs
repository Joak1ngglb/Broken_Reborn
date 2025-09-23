using System;
using Intersect.Server.Entities;

namespace Intersect.Server.Entities.Pets;

/// <summary>
///     Lightweight behavioural component responsible for keeping track of a
///     pet's wellbeing metrics (energy/mood) and reacting to owner-driven
///     interactions such as feeding or fulfilling whims.
/// </summary>
internal sealed class PetCareBrain
{
    private readonly IPet _pet;
    private readonly object _syncRoot = new();

    private long _lastTick;

    private const long EnergyTickIntervalMs = 15_000;
    private const int EnergyDecayPerTick = 1;
    private const int LowEnergyMoodPenalty = 1;

    private const int FeedEnergyBonus = 15;
    private const int FeedMoodBonus = 5;

    private const int PettingMoodBonus = 8;

    private const int WhimEnergyBonus = 10;
    private const int WhimMoodBonus = 12;

    public PetCareBrain(IPet pet)
    {
        _pet = pet ?? throw new ArgumentNullException(nameof(pet));
    }

    public void Update(long timeMs)
    {
        lock (_syncRoot)
        {
            if (_lastTick == 0)
            {
                _lastTick = timeMs;
                return;
            }

            var elapsed = timeMs - _lastTick;
            if (elapsed < EnergyTickIntervalMs)
            {
                return;
            }

            var ticks = (int)(elapsed / EnergyTickIntervalMs);
            if (ticks <= 0)
            {
                return;
            }

            _lastTick += ticks * EnergyTickIntervalMs;

            var energyLoss = ticks * EnergyDecayPerTick;
            if (energyLoss <= 0)
            {
                return;
            }

            var energyChanged = _pet.ModifyEnergy(-energyLoss);
            if (!energyChanged && _pet.Energy > 0)
            {
                return;
            }

            if (_pet.Energy <= 0)
            {
                _pet.ModifyMoodValue(-ticks * LowEnergyMoodPenalty);
            }
        }
    }

    public void RegisterFeeding()
    {
        lock (_syncRoot)
        {
            _pet.ModifyEnergy(FeedEnergyBonus);
            _pet.ModifyMoodValue(FeedMoodBonus);
        }
    }

    public void RegisterPetting()
    {
        lock (_syncRoot)
        {
            _pet.ModifyMoodValue(PettingMoodBonus);
        }
    }

    public void RegisterWhimFulfilled()
    {
        lock (_syncRoot)
        {
            _pet.ModifyEnergy(WhimEnergyBonus);
            _pet.ModifyMoodValue(WhimMoodBonus);
            _pet.RegisterWhimFulfillment();
        }
    }
}
