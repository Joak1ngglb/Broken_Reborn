using System;
using Intersect.Enums;

namespace Intersect.Server.Entities;

/// <summary>
///     Minimal contract for server-side pet entities so that auxiliary
///     components (brains, services, persistence helpers) can operate without
///     depending on the concrete <see cref="Pet"/> implementation.
/// </summary>
public interface IPet
{
    /// <summary>
    ///     Gets the current energy of the pet, expressed on the same scale as
    ///     the descriptor's base energy.
    /// </summary>
    int Energy { get; }

    /// <summary>
    ///     Gets the raw mood value used internally to evaluate emotional
    ///     thresholds.
    /// </summary>
    int MoodValue { get; }

    /// <summary>
    ///     Gets the coarse emotional state derived from <see cref="MoodValue"/>.
    /// </summary>
    PetMood Mood { get; }

    /// <summary>
    ///     Gets the number of whims fulfilled by the owner during the current
    ///     session. This value is persisted for analytics and balancing.
    /// </summary>
    int WhimsFulfilled { get; }

    /// <summary>
    ///     Gets the UTC timestamp (in ticks) of the last whim fulfillment. A
    ///     value of <c>0</c> indicates that no whim has been fulfilled yet.
    /// </summary>
    long LastWhimFulfillmentTicks { get; }

    /// <summary>
    ///     Adjusts the pet's energy by the specified delta while clamping the
    ///     result to the allowed range.
    /// </summary>
    /// <param name="delta">Amount of energy to add (positive) or remove (negative).</param>
    /// <param name="persist">Whether the change should be written to the persistence layer immediately.</param>
    /// <param name="notify">Whether the owner should be notified of the change.</param>
    /// <returns><c>true</c> if the energy value changed; otherwise <c>false</c>.</returns>
    bool ModifyEnergy(int delta, bool persist = true, bool notify = true);

    /// <summary>
    ///     Adjusts the internal mood value by the supplied delta.
    /// </summary>
    /// <param name="delta">Amount of mood to add (positive) or remove (negative).</param>
    /// <param name="persist">Whether to persist the new value immediately.</param>
    /// <param name="notify">Whether to send an update packet to the owner.</param>
    /// <returns><c>true</c> if the mood value changed; otherwise <c>false</c>.</returns>
    bool ModifyMoodValue(int delta, bool persist = true, bool notify = true);

    /// <summary>
    ///     Records that the owner has fulfilled one of the pet's whims. This is
    ///     used to drive mood/energy bonuses and persistence statistics.
    /// </summary>
    /// <param name="persist">Whether to persist the counters immediately.</param>
    /// <param name="notify">Whether to notify the owner of the change.</param>
    void RegisterWhimFulfillment(bool persist = true, bool notify = true);
}
