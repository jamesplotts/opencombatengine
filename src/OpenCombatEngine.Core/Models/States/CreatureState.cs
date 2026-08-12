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
    /// Serializable state for an action economy component.
    /// </summary>
    /// <param name="HasAction">Whether the Action is still available this turn.</param>
    /// <param name="HasBonusAction">Whether the Bonus Action is still available this turn.</param>
    /// <param name="HasReaction">Whether the Reaction is still available.</param>
    public record ActionEconomyState(
        bool HasAction,
        bool HasBonusAction,
        bool HasReaction);

    /// <summary>
    /// Serializable state for a creature.
    /// </summary>
    /// <param name="Id">Unique identifier.</param>
    /// <param name="Name">Creature name.</param>
    /// <param name="Team">Team assignment.</param>
    /// <param name="AbilityScores">State of ability scores.</param>
    /// <param name="HitPoints">State of hit points.</param>
    /// <param name="CombatStats">State of combat stats.</param>
    /// <param name="Conditions">State of conditions.</param>
    /// <param name="LevelManager">State of level manager.</param>
    /// <param name="ActionEconomy">State of action economy (Action/Bonus Action/Reaction availability).</param>
    /// <param name="Inventory">State of the inventory (items owned by the creature).</param>
    /// <param name="Equipment">State of equipped/attuned items, referencing <paramref name="Inventory"/> by index.</param>
    /// <param name="Spellcasting">State of the spellcasting component, if the creature is a caster.</param>
    public record CreatureState(
        Guid Id,
        string Name,
        string Team,
        AbilityScoresState AbilityScores,
        HitPointsState HitPoints,
        CombatStatsState? CombatStats = null,
        ConditionManagerState? Conditions = null,
        LevelManagerState? LevelManager = null,
        ActionEconomyState? ActionEconomy = null,
        InventoryState? Inventory = null,
        EquipmentState? Equipment = null,
        SpellCasterState? Spellcasting = null);
}
