using System;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for an ability scores component.
    /// </summary>
    /// <param name="Strength">Strength score.</param>
    /// <param name="Dexterity">Dexterity score.</param>
    /// <param name="Constitution">Constitution score.</param>
    /// <param name="Intelligence">Intelligence score.</param>
    /// <param name="Wisdom">Wisdom score.</param>
    /// <param name="Charisma">Charisma score.</param>
    public record AbilityScoresState(
        int Strength,
        int Dexterity,
        int Constitution,
        int Intelligence,
        int Wisdom,
        int Charisma);

    /// <summary>
    /// Serializable state for a hit points component.
    /// </summary>
    /// <param name="Current">Current hit points.</param>
    /// <param name="Max">Maximum hit points.</param>
    /// <param name="Temporary">Temporary hit points.</param>
    public record HitPointsState(
        int Current,
        int Max,
        int Temporary);

    /// <summary>
    /// Serializable state for a creature.
    /// </summary>
    /// <param name="Id">Unique identifier.</param>
    /// <param name="Name">Creature name.</param>
    /// <param name="AbilityScores">State of ability scores.</param>
    /// <param name="HitPoints">State of hit points.</param>
    /// <param name="CombatStats">State of combat stats.</param>
    public record CreatureState(
        Guid Id,
        string Name,
        AbilityScoresState AbilityScores,
        HitPointsState HitPoints,
        CombatStatsState? CombatStats = null); // Optional for backward compatibility if needed, though we are in dev.
}
