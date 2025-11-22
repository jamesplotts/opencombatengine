using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Spells
{
    public interface ISpell
    {
        string Name { get; }
        int Level { get; }
        SpellSchool School { get; }
        string CastingTime { get; } // e.g. "1 Action", "1 Bonus Action"
        string Range { get; } // e.g. "60 feet", "Touch"
        string Components { get; } // e.g. "V, S, M"
        string Duration { get; } // e.g. "Instantaneous", "1 minute"
        string Description { get; }

        /// <summary>
        /// Executes the spell's effect.
        /// </summary>
        /// <param name="caster">The creature casting the spell.</param>
        /// <param name="target">The target of the spell (optional).</param>
        /// <returns>Result of the cast.</returns>
        Result<bool> Cast(ICreature caster, object? target = null);
    }
}
