using System.Collections.ObjectModel;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a single spell slot level.
    /// </summary>
    /// <param name="Level">Spell slot level (1-9).</param>
    /// <param name="Max">Maximum slots at this level.</param>
    /// <param name="Current">Slots currently available at this level.</param>
    public record SpellSlotState(int Level, int Max, int Current);

    /// <summary>
    /// Serializable state for a spellcaster component.
    /// </summary>
    /// <param name="CastingAbility">The ability score used for spellcasting.</param>
    /// <param name="IsPreparedCaster">Whether the caster prepares spells from a known list (e.g. Cleric/Wizard) rather than always having all known spells ready (e.g. Sorcerer).</param>
    /// <param name="KnownSpellNames">Names of known spells, resolved via an <see cref="OpenCombatEngine.Core.Interfaces.Spells.ISpellRepository"/> on restore.</param>
    /// <param name="PreparedSpellNames">Names of currently prepared spells.</param>
    /// <param name="Slots">Spell slot levels and their max/current counts.</param>
    /// <param name="PactSlotsMax">Maximum Pact Magic slots (Warlock).</param>
    /// <param name="PactSlotsCurrent">Currently available Pact Magic slots.</param>
    /// <param name="PactSlotLevel">The level of Pact Magic slots.</param>
    /// <param name="ConcentratingOnSpellName">Name of the spell currently being concentrated on, if any.</param>
    public record SpellCasterState(
        Ability CastingAbility,
        bool IsPreparedCaster,
        Collection<string> KnownSpellNames,
        Collection<string> PreparedSpellNames,
        Collection<SpellSlotState> Slots,
        int PactSlotsMax,
        int PactSlotsCurrent,
        int PactSlotLevel,
        string? ConcentratingOnSpellName = null);
}
