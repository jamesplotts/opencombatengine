using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Interfaces.Creatures;

/// <summary>
/// Defines the contract for a creature's ability scores.
/// </summary>
public interface IAbilityScores
{
    /// <summary>
    /// Gets the Strength score.
    /// </summary>
    int Strength { get; }

    /// <summary>
    /// Gets the Dexterity score.
    /// </summary>
    int Dexterity { get; }

    /// <summary>
    /// Gets the Constitution score.
    /// </summary>
    int Constitution { get; }

    /// <summary>
    /// Gets the Intelligence score.
    /// </summary>
    int Intelligence { get; }

    /// <summary>
    /// Gets the Wisdom score.
    /// </summary>
    int Wisdom { get; }

    /// <summary>
    /// Gets the Charisma score.
    /// </summary>
    int Charisma { get; }

    /// <summary>
    /// Calculates the modifier for a given ability score.
    /// </summary>
    /// <param name="ability">The ability to calculate the modifier for.</param>
    /// <returns>The calculated modifier.</returns>
    int GetModifier(Ability ability);
}
