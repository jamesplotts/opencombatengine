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
    /// <param name="ChallengeRating">
    /// SRD challenge rating (0, 1/8, 1/4, 1/2, or a whole number up to 30).
    /// Meaningful for a monster/NPC record (set by the DM at creation
    /// time, the same way every other stat on that record is authored);
    /// null for a player character, which has no CR in 5e, and for any
    /// creature the DM never assigned one. Deliberately nullable rather
    /// than defaulting to 0, since 0 is itself a real, valid SRD CR (a
    /// commoner, a rat) — collapsing "never set" into 0 would silently
    /// make an un-authored creature loot-eligible. Feeds the GenerateLoot
    /// RPC's encounter-CR computation — a creature with no CR recorded
    /// (null) cannot be included in a loot roll.
    /// </param>
    /// <param name="Gender">
    /// Free-text roleplay flavor — the SRD attaches no mechanical effect
    /// to gender, so this is never validated against an enum or read by
    /// any rules logic. Null for a creature nobody set one on (every
    /// existing record before this field existed, and any DM-authored
    /// monster/NPC that doesn't care to set it). Appended as the last
    /// parameter (not grouped near Spellcasting) so every pre-existing
    /// positional call site in this repo keeps compiling unchanged.
    /// </param>
    /// <param name="RaceName">
    /// The SRD race name the character was created as ("Human", "Elf",
    /// "Dwarf", "Halfling"). The mechanical racial effects are already
    /// baked into ability scores / features at creation; this is the
    /// plain string kept purely so a downstream consumer can display
    /// "Male Dwarven Fighter" without re-deriving it. Null for a creature
    /// nobody set one on (every DM-authored monster/NPC, every record
    /// before this field). Appended last, same reasoning as Gender.
    /// </param>
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
        SpellCasterState? Spellcasting = null,
        double? ChallengeRating = null,
        string? Gender = null,
        string? RaceName = null);
}
