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

        bool RequiresAttackRoll { get; }
        bool RequiresConcentration { get; }
        Ability? SaveAbility { get; } // Null if no save
        OpenCombatEngine.Core.Enums.SaveEffect SaveEffect { get; }
        
        // Deprecating single DamageDice/Type in favor of list
        // string? DamageDice { get; } 
        // DamageType? DamageType { get; }
        
        System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.DamageFormula> DamageRolls { get; }
        string? HealingDice { get; }
        System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.SpellConditionDefinition> AppliedConditions { get; }

        /// <summary>
        /// How many times DamageRolls is independently rolled and applied
        /// per cast at this spell's own base Level (e.g. 3 for Magic
        /// Missile's three darts, each separately rolled — SRD "you create
        /// three ... darts"). 1 for the overwhelming majority of spells,
        /// which only ever affect a target once.
        /// </summary>
        int InstanceCount { get; }

        /// <summary>
        /// Additional InstanceCount gained per spell slot level this spell
        /// is cast above its own base Level (SRD "one more dart/ray for
        /// each slot level above Nth" upcast text). 0 for a spell whose
        /// InstanceCount doesn't scale with upcasting.
        /// </summary>
        int InstanceCountPerUpcastLevel { get; }
        
        OpenCombatEngine.Core.Interfaces.Spatial.IShape? AreaOfEffect { get; }

        /// <summary>
        /// Executes the spell's effect.
        /// </summary>
        /// <param name="caster">The creature casting the spell.</param>
        /// <param name="target">The target of the spell (optional).</param>
        /// <returns>Result of the cast.</returns>
        Result<OpenCombatEngine.Core.Models.Spells.SpellResolution> Cast(ICreature caster, object? target = null);
    }
}
