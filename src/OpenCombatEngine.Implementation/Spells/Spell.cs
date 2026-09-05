using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Spells
{
    public class Spell : ISpell
    {
        public string Name { get; }
        public int Level { get; }
        public SpellSchool School { get; }
        public string CastingTime { get; }
        public string Range { get; }
        public string Components { get; }
        public string Duration { get; }
        public string Description { get; }
        
        public bool RequiresAttackRoll { get; }
        public bool RequiresConcentration { get; }
        public Ability? SaveAbility { get; }
        public OpenCombatEngine.Core.Enums.SaveEffect SaveEffect { get; }
        
        public System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.DamageFormula> DamageRolls { get; }
        public string? HealingDice { get; }

        public System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.SpellConditionDefinition> AppliedConditions { get; }

        public int InstanceCount { get; }
        public int InstanceCountPerUpcastLevel { get; }
        
        public OpenCombatEngine.Core.Interfaces.Spatial.IShape? AreaOfEffect { get; }

        public System.Collections.Generic.IReadOnlyList<string> Classes { get; }

        private readonly OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller _diceRoller;
        // private readonly Func<ICreature, object?, Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>>? _customEffect;

        public Spell(
            string name, 
            int level, 
            SpellSchool school, 
            string castingTime, 
            string range, 
            string components, 
            string duration, 
            string description,
            OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller diceRoller,
            bool requiresAttackRoll = false,
            bool requiresConcentration = false,
            Ability? saveAbility = null,
            OpenCombatEngine.Core.Enums.SaveEffect saveEffect = OpenCombatEngine.Core.Enums.SaveEffect.None,
            System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.DamageFormula>? damageRolls = null,
            string? healingDice = null,
            System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.SpellConditionDefinition>? appliedConditions = null,
            OpenCombatEngine.Core.Interfaces.Spatial.IShape? areaOfEffect = null,
            int instanceCount = 1,
            int instanceCountPerUpcastLevel = 0,
            System.Collections.Generic.IReadOnlyList<string>? classes = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (level < 0 || level > 9) throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 0 and 9");
            if (instanceCount < 1) throw new ArgumentOutOfRangeException(nameof(instanceCount), "InstanceCount must be at least 1.");
            if (instanceCountPerUpcastLevel < 0) throw new ArgumentOutOfRangeException(nameof(instanceCountPerUpcastLevel), "InstanceCountPerUpcastLevel cannot be negative.");
            ArgumentNullException.ThrowIfNull(diceRoller);

            Name = name;
            Level = level;
            School = school;
            CastingTime = castingTime;
            Range = range;
            Components = components;
            Duration = duration;
            Description = description;
            _diceRoller = diceRoller;
            RequiresAttackRoll = requiresAttackRoll;
            RequiresConcentration = requiresConcentration;
            SaveAbility = saveAbility;
            SaveEffect = saveEffect;
            DamageRolls = damageRolls ?? new System.Collections.Generic.List<OpenCombatEngine.Core.Models.Spells.DamageFormula>();
            HealingDice = healingDice;
            AppliedConditions = appliedConditions ?? new System.Collections.Generic.List<OpenCombatEngine.Core.Models.Spells.SpellConditionDefinition>();
            AreaOfEffect = areaOfEffect;
            InstanceCount = instanceCount;
            InstanceCountPerUpcastLevel = instanceCountPerUpcastLevel;
            Classes = classes ?? new System.Collections.Generic.List<string>();
        }

        public Result<OpenCombatEngine.Core.Models.Spells.SpellResolution> Cast(ICreature caster, object? target = null)
        {
            if (caster == null) return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Failure("Caster cannot be null.");
            
            // Default Resolution
            // We now handle effects in CastSpellAction via ApplySpellEffects.
            // Spell.Cast just validates and returns success message.
            
            return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(new OpenCombatEngine.Core.Models.Spells.SpellResolution(true, "Cast successfully."));
        }
    }
}
