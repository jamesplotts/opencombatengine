using System;
using System.Collections.ObjectModel;
using OpenCombatEngine.Core.Enums;

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
    /// <param name="HasFreeObjectInteraction">
    /// Whether this turn's free object interaction (draw/sheathe a weapon, or
    /// similar) is still available — a fourth, smaller resource distinct from
    /// the three above (see <see cref="OpenCombatEngine.Core.Interfaces.Creatures.IActionEconomy"/>).
    /// Defaults to true so a record predating this field (deserialized from
    /// JSON with the property absent) restores as "available," the same
    /// backward-compatible-optional-field convention every other addition to
    /// this project's state records already follows. Appended last so every
    /// pre-existing positional call site keeps compiling unchanged.
    /// </param>
    public record ActionEconomyState(
        bool HasAction,
        bool HasBonusAction,
        bool HasReaction,
        bool HasFreeObjectInteraction = true);

    /// <summary>
    /// Serializable state for a check-manager component: which skills and
    /// saving throws a creature is proficient in. Added to close a real,
    /// live-observed gap — <c>StandardCheckManager</c>'s own proficiency
    /// sets were never persisted at all before this (no state class, no
    /// restore path), so every proficiency granted at character creation
    /// silently vanished on the very next save/load or gRPC round trip,
    /// the same bug class <see cref="ActionEconomyState"/>/Gender/RaceName/
    /// Background were each already fixed for individually.
    /// </summary>
    /// <param name="SkillProficiencies">
    /// Skill names (e.g. "Persuasion", "Athletics") this creature is
    /// proficient in — matches whatever string
    /// <see cref="OpenCombatEngine.Core.Interfaces.Creatures.ICheckManager.AddSkillProficiency"/>
    /// was called with, compared case-insensitively at lookup time.
    /// </param>
    /// <param name="SavingThrowProficiencies">
    /// Abilities this creature is proficient in saving throws for.
    /// </param>
    public record CheckManagerState(
        Collection<string> SkillProficiencies,
        Collection<Ability> SavingThrowProficiencies);

    /// <summary>
    /// One ability score with its already-computed SRD modifier
    /// (<c>floor((Score-10)/2)</c>) — a purely derived, display-oriented
    /// entry. Exists so a schema-driven client (Layforge's character
    /// sheet) can render a Name/Score/Mod table without knowing the SRD
    /// modifier formula itself: the engine that owns the rule computes
    /// it, the client just displays whatever array of these it's given.
    /// Built by the implementation layer's own creature state export (in
    /// real STR/DEX/CON/INT/WIS/CHA order, not derived from this record's
    /// own field order — JSON array order is what a client actually
    /// sees) — this project's Core assembly has no reference back to
    /// that layer, so it can't be linked here directly.
    /// </summary>
    /// <param name="Name">The ability's full name ("Strength").</param>
    /// <param name="Score">The raw ability score.</param>
    /// <param name="Modifier">The already-computed SRD modifier.</param>
    public record AbilityEntry(string Name, int Score, int Modifier);

    /// <summary>
    /// One SRD skill with its already-computed total modifier — same
    /// "engine computes, client just displays" reasoning as
    /// <see cref="AbilityEntry"/>. <see cref="Ability"/> is deliberately
    /// the 3-letter abbreviation ("STR"/"DEX"/...), not the full ability
    /// name, for compact display in a narrow sheet column.
    /// </summary>
    /// <param name="Name">The SRD skill name ("Persuasion").</param>
    /// <param name="Ability">The governing ability's 3-letter abbreviation.</param>
    /// <param name="Proficient">Whether this creature is proficient in this skill.</param>
    /// <param name="Modifier">
    /// The total modifier a real check with this skill would add beyond
    /// the d20 face — ability modifier, plus the proficiency bonus when
    /// <paramref name="Proficient"/>, plus any active general
    /// ability-check bonus (a buff, a feat, a magic item implemented as
    /// one) via the same effects hook a real roll applies. This engine
    /// has no per-skill-specific bonus mechanism today (only whole-
    /// ability-check/whole-saving-throw granularity) — an item or feat
    /// that boosts one named skill only has no representation here yet;
    /// this value is exactly what the engine can currently compute, never
    /// an invented approximation of what it can't.
    /// </param>
    public record SkillEntry(string Name, string Ability, bool Proficient, int Modifier);

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
    /// <param name="Background">
    /// The SRD background the character was created with ("Criminal",
    /// "Sage", "Acolyte", ...). Its mechanical effects (starting
    /// equipment/gold, and any skill proficiencies a future feature adds)
    /// are already baked in at creation the same way race's are; this is
    /// the plain string kept so a downstream consumer — a DM composing a
    /// character's personal introduction, a character sheet — can read
    /// and roleplay from it without re-deriving which background was
    /// chosen. Null for a creature nobody set one on (every DM-authored
    /// monster/NPC, every record before this field). Appended last, same
    /// reasoning as Gender/RaceName.
    /// </param>
    /// <param name="Checks">
    /// Skill/saving-throw proficiencies (see <see cref="CheckManagerState"/>'s
    /// own doc comment for the bug this closes). Null for a creature with
    /// none recorded (every record before this field, or one restored from
    /// a save that predates it) — restores as "no proficiencies," not an
    /// error. Appended last, same reasoning as Gender/RaceName/Background.
    /// </param>
    /// <param name="Abilities">
    /// The six ability scores with their computed SRD modifiers, in real
    /// STR/DEX/CON/INT/WIS/CHA order — purely derived from
    /// <paramref name="AbilityScores"/>, recomputed fresh on every
    /// <c>GetState()</c> rather than stored independently, so it can never
    /// drift out of sync with it. Exists only so a schema-driven client
    /// can render a Name/Score/Mod table without its own copy of the SRD
    /// modifier formula. Null on a restore path that doesn't recompute it
    /// (e.g. a hand-built <see cref="CreatureState"/> in a test) — never
    /// read by any rules logic, display-only.
    /// </param>
    /// <param name="Skills">
    /// All 18 SRD skills with their computed total modifiers — same
    /// "derived, display-only, recomputed every <c>GetState()</c>"
    /// reasoning as <paramref name="Abilities"/>. See
    /// <see cref="SkillEntry"/>'s own doc comment for exactly what the
    /// modifier does and doesn't include.
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
        string? RaceName = null,
        string? Background = null,
        CheckManagerState? Checks = null,
        Collection<AbilityEntry>? Abilities = null,
        Collection<SkillEntry>? Skills = null);
}
