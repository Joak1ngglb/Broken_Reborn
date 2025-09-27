using System;
using Intersect.Framework.Core.GameObjects.Pets;
using Intersect.Server.Entities;
using Intersect.Utilities;

namespace Intersect.Server.Entities.Pets;

/// <summary>
/// Handles care-related interactions such as feeding, petting and fulfilling whims.
/// This class centralises the logic so <see cref="Pet"/> remains focused on combat and AI state.
/// </summary>
public sealed class PetCareBrain
{
    private readonly Pet _pet;

    private long _lastFedAt;
    private long _lastAffectionAt;

    private const long FeedCooldown = 1500;
    private const long AffectionCooldown = 1200;

    public PetCareBrain(Pet pet)
    {
        _pet = pet ?? throw new ArgumentNullException(nameof(pet));
    }

    /// <summary>
    /// Adjusts the pet energy and mood when the owner feeds it. Returns <c>true</c> when
    /// the interaction was accepted (i.e. passed cooldown and resulted in a state change).
    /// </summary>
    public bool TryRegisterFeeding(int energyDelta, int moodDelta)
    {
        if (!CanInteract(ref _lastFedAt, FeedCooldown))
        {
            return false;
        }

        var changed = _pet.ApplyCareDeltas(
            Math.Clamp(energyDelta, -Pet.MaxAttributeValue, Pet.MaxAttributeValue),
            Math.Clamp(moodDelta, -Pet.MaxAttributeValue, Pet.MaxAttributeValue)
        );

        if (!changed)
        {
            return false;
        }

        _pet.FinalizeCareApplication(incrementWhim: false);
        return true;
    }

    /// <summary>
    /// Applies a mood bonus when the owner pets/cuddles the companion.
    /// </summary>
    public bool TryRegisterAffection(int moodDelta)
    {
        if (!CanInteract(ref _lastAffectionAt, AffectionCooldown))
        {
            return false;
        }

        var changed = _pet.ApplyCareDeltas(0, Math.Clamp(moodDelta, -Pet.MaxAttributeValue, Pet.MaxAttributeValue));
        if (!changed)
        {
            return false;
        }

        _pet.FinalizeCareApplication(incrementWhim: false);
        return true;
    }

    /// <summary>
    /// Records that the player satisfied a whim. This always increments the whim counter and
    /// provides a modest energy/mood boost capped by descriptor defaults.
    /// </summary>
    public void RegisterWhimFulfillment()
    {
        var descriptor = _pet.Descriptor;
        var energyBonus = descriptor != null
            ? Math.Max(1, descriptor.BaseEnergy / 8)
            : 5;
        var moodBonus = descriptor != null
            ? Math.Max(1, descriptor.BaseMood / 6)
            : 5;

        _ = _pet.ApplyCareDeltas(energyBonus, moodBonus);
        _pet.FinalizeCareApplication(incrementWhim: true);
    }

    private static bool CanInteract(ref long timestamp, long cooldown)
    {
        var now = Timing.Global.Milliseconds;
        if (now < timestamp + cooldown)
        {
            return false;
        }

        timestamp = now;
        return true;
    }
}
