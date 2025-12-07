using System;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Spells;

namespace OpenCombatEngine.Implementation.Spells
{
    public static class SpellValidationService
    {
        public static bool IsSpellValidForClass(IClassDefinition classDefinition, ISpell spell)
        {
            ArgumentNullException.ThrowIfNull(classDefinition);
            ArgumentNullException.ThrowIfNull(spell);

            // If class has no spell list defined, assume no restriction (or no spells allowed? debated).
            // For now, if SpellList is null, it typically means it's not a spellcasting class or data is missing.
            // Let's return false if list is missing to be safe, or true if we treat null as "Access All"?
            // A fighter has no spell list. So IsSpellValidForClass(Fighter, Fireball) should be false.
            if (classDefinition.SpellList == null) return false;

            return classDefinition.SpellList.Contains(spell.Name);
        }
    }
}
