using System;

namespace Intersect.Framework.Core.GameObjects.Pets;

/// <summary>
/// Represents the simplified emotional state of a pet based on its raw mood attribute.
/// The values are ordered from worst to best so comparisons can use relational operators.
/// </summary>
public enum PetMood : byte
{
    /// <summary>
    /// The pet is completely unresponsive and needs rest before interacting again.
    /// </summary>
    Exhausted = 0,

    /// <summary>
    /// The pet is grumpy and will only respond to basic care.
    /// </summary>
    Irritable = 1,

    /// <summary>
    /// The pet feels neutral and will respond to normal commands.
    /// </summary>
    Content = 2,

    /// <summary>
    /// The pet is happy and gains small bonuses from interactions.
    /// </summary>
    Happy = 3,

    /// <summary>
    /// The pet is delighted and rewards players for keeping it satisfied.
    /// </summary>
    Elated = 4,
}

/// <summary>
/// Helper utilities to convert between raw attribute values and <see cref="PetMood"/> states.
/// </summary>
public static class PetMoodUtility
{
    private const int DefaultAttributeCap = 100;

    /// <summary>
    /// Converts a raw mood attribute value into an enum state.
    /// </summary>
    /// <param name="value">Current mood attribute reported by the server.</param>
    /// <param name="maximum">Optional attribute cap used by the pet descriptor.</param>
    /// <returns>The interpreted <see cref="PetMood"/>.</returns>
    public static PetMood FromAttribute(int value, int maximum = DefaultAttributeCap)
    {
        if (maximum <= 0)
        {
            maximum = DefaultAttributeCap;
        }

        var normalized = Math.Clamp(value, 0, maximum) / (double)maximum;

        return normalized switch
        {
            <= 0.1 => PetMood.Exhausted,
            <= 0.35 => PetMood.Irritable,
            <= 0.65 => PetMood.Content,
            <= 0.9 => PetMood.Happy,
            _ => PetMood.Elated,
        };
    }

    /// <summary>
    /// Determines whether the supplied mood requires restricting high energy actions.
    /// </summary>
    public static bool RequiresRest(PetMood mood) => mood <= PetMood.Irritable;
}
