namespace OpenCombatEngine.Core.Enums;

/// <summary>
/// Represents the six standard ability scores for a creature.
/// </summary>
public enum Ability
{
    /// <summary>
    /// Default value, should not be used.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Measuring physical power.
    /// </summary>
    Strength,

    /// <summary>
    /// Measuring agility.
    /// </summary>
    Dexterity,

    /// <summary>
    /// Measuring endurance.
    /// </summary>
    Constitution,

    /// <summary>
    /// Measuring reasoning and memory.
    /// </summary>
    Intelligence,

    /// <summary>
    /// Measuring perception and insight.
    /// </summary>
    Wisdom,

    /// <summary>
    /// Measuring force of personality.
    /// </summary>
    Charisma,

    /// <summary>
    /// Sentinel value for validation.
    /// </summary>
    LastValue
}
