using System.Collections.Generic;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Spells
{
    public interface ISpellCaster
    {
        /// <summary>
        /// Gets the list of spells known by the caster.
        /// </summary>
        IReadOnlyList<ISpell> KnownSpells { get; }

        /// <summary>
        /// Gets the list of spells currently prepared.
        /// If null, the caster uses KnownSpells (e.g. Sorcerer).
        /// </summary>
        IReadOnlyList<ISpell> PreparedSpells { get; }

        /// <summary>
        /// Gets the Spell Save DC.
        /// </summary>
        int SpellSaveDC { get; }

        /// <summary>
        /// Gets the Spell Attack Bonus.
        /// </summary>
        int SpellAttackBonus { get; }

        /// <summary>
        /// Checks if the caster has a slot available for the given level.
        /// </summary>
        bool HasSlot(int level);

        /// <summary>
        /// Gets the current number of slots available for the given level.
        /// </summary>
        int GetSlots(int level);

        /// <summary>
        /// Gets the maximum number of slots for the given level.
        /// </summary>
        int GetMaxSlots(int level);

        /// <summary>
        /// Consumes a spell slot of the given level.
        /// </summary>
        Result<bool> ConsumeSlot(int level);

        /// <summary>
        /// Restores all spell slots (Long Rest).
        /// </summary>
        void RestoreAllSlots();

        /// <summary>
        /// Adds a spell to the known spells list.
        /// </summary>
        void LearnSpell(ISpell spell);

        /// <summary>
        /// Prepares a spell from the known spells list.
        /// </summary>
        Result<bool> PrepareSpell(ISpell spell);

        /// <summary>
        /// Unprepares a spell.
        /// </summary>
        void UnprepareSpell(ISpell spell);
        
        /// <summary>
        /// Sets the number of slots for a level (e.g. on level up).
        /// </summary>
        void SetSlots(int level, int max);

        /// <summary>
        /// Sets the effect manager for calculating dynamic bonuses.
        /// </summary>
        void SetEffectManager(OpenCombatEngine.Core.Interfaces.Effects.IEffectManager effects);

        /// <summary>
        /// Gets the spell currently being concentrated on, if any.
        /// </summary>
        ISpell? ConcentratingOn { get; }

        /// <summary>
        /// Breaks concentration on the current spell.
        /// </summary>
        void BreakConcentration();

        /// <summary>
        /// Sets concentration on a spell, breaking any existing concentration.
        /// </summary>
        /// <param name="spell">The spell to concentrate on.</param>
        void SetConcentration(ISpell spell);
    }
}
