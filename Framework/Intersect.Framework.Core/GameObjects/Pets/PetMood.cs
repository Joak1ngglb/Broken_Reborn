namespace Intersect.Enums;

/// <summary>
///     Represents the coarse emotional state of a pet as exposed to both the
///     server and client. The mood derives from the underlying numeric mood
///     value and is used by gameplay systems to gate interactions.
/// </summary>
public enum PetMood
{
    /// <summary>
    ///     The pet is distressed and unwilling to cooperate. Triggered when the
    ///     mood value is critically low.
    /// </summary>
    Miserable = 0,

    /// <summary>
    ///     The pet is displeased and requires attention before taking on
    ///     demanding tasks.
    /// </summary>
    Irritable = 1,

    /// <summary>
    ///     The pet feels neutral. It will obey standard commands but is not
    ///     particularly enthusiastic.
    /// </summary>
    Content = 2,

    /// <summary>
    ///     The pet is happy and eager to interact with its owner.
    /// </summary>
    Happy = 3,

    /// <summary>
    ///     The pet is thrilled. This is the best possible emotional state.
    /// </summary>
    Joyful = 4,
}
