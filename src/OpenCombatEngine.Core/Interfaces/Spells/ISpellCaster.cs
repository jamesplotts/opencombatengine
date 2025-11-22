using System.Collections.Generic;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Spells
{
    public interface ISpellCaster
    {
        /// <summary>
        /// Gets the list of spells known or prepared by the caster.
        /// </summary>
        IReadOnlyList<ISpell> KnownSpells { get; }

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
        /// Sets the number of slots for a level (e.g. on level up).
        /// </summary>
        void SetSlots(int level, int max);
    }
}
