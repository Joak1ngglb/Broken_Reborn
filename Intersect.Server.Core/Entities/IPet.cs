using Intersect.Framework.Core.GameObjects.Pets;

namespace Intersect.Server.Entities;

/// <summary>
/// Defines the minimum contract exposed by pet entities so that ancillary systems can
/// interact with them without depending on the concrete <see cref="Pet"/> implementation.
/// </summary>
public interface IPet
{
    /// <summary>
    /// Gets the current energy pool for the pet. This value is persisted and replicated to clients.
    /// </summary>
    int Energy { get; }

    /// <summary>
    /// Gets the qualitative mood derived from the raw mood attribute for UI and logic purposes.
    /// </summary>
    PetMood Mood { get; }

    /// <summary>
    /// Gets the raw mood attribute as tracked by progression systems.
    /// </summary>
    int MoodValue { get; }

    /// <summary>
    /// Gets the number of whims that have been fulfilled since the pet was summoned or loaded.
    /// </summary>
    int WhimsFulfilled { get; }

    /// <summary>
    /// Registers that the owner completed one of the pet's whims, updating internal counters
    /// and triggering any necessary mood/energy side effects.
    /// </summary>
    void RegisterWhimFulfillment();
}
