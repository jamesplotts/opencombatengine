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
        public string? DamageDice { get; }
        public DamageType? DamageType { get; }
        public OpenCombatEngine.Core.Interfaces.Spatial.IShape? AreaOfEffect { get; }

        private readonly OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller _diceRoller;
        private readonly Func<ICreature, object?, Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>>? _customEffect;

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
            string? damageDice = null,
            DamageType? damageType = null,
            OpenCombatEngine.Core.Interfaces.Spatial.IShape? areaOfEffect = null,
            Func<ICreature, object?, Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>>? customEffect = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (level < 0 || level > 9) throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 0 and 9");
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
            _diceRoller = diceRoller;
            RequiresAttackRoll = requiresAttackRoll;
            RequiresConcentration = requiresConcentration;
            SaveAbility = saveAbility;
            DamageDice = damageDice;
            DamageType = damageType;
            AreaOfEffect = areaOfEffect;
            _customEffect = customEffect;
        }

        public Result<OpenCombatEngine.Core.Models.Spells.SpellResolution> Cast(ICreature caster, object? target = null)
        {
            if (caster == null) return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Failure("Caster cannot be null.");
            
            if (_customEffect != null)
            {
                return _customEffect(caster, target);
            }

            // Default Resolution Logic
            OpenCombatEngine.Core.Models.Combat.AttackResult? attackResult = null;
            bool? saveSucceeded = null;
            int damageDealt = 0;
            string message = $"Cast {Name}.";

            // 1. Attack Roll
            if (RequiresAttackRoll)
            {
                if (target is not ICreature targetCreature)
                    return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Failure("Target must be a creature for attack spells.");

                var attackBonus = caster.Spellcasting?.SpellAttackBonus ?? 0;
                var d20 = _diceRoller.Roll("1d20");
                if (!d20.IsSuccess) return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Failure("Failed to roll d20.");

                int rollVal = d20.Value.Total;
                bool isCrit = rollVal == 20;
                bool isCritFail = rollVal == 1;
                int totalToHit = rollVal + attackBonus;
                
                bool hits = !isCritFail && (isCrit || totalToHit >= targetCreature.CombatStats.ArmorClass);
                
                attackResult = new OpenCombatEngine.Core.Models.Combat.AttackResult(
                    caster,
                    targetCreature,
                    rollVal,
                    isCrit,
                    false, // HasAdvantage
                    false, // HasDisadvantage
                    new System.Collections.Generic.List<OpenCombatEngine.Core.Models.Combat.DamageRoll>()
                );

                if (!hits)
                {
                    return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(
                        new OpenCombatEngine.Core.Models.Spells.SpellResolution(true, "Attack missed.", attackResult));
                }
            }

            // 2. Saving Throw
            if (SaveAbility.HasValue)
            {
                if (target is not ICreature targetCreature)
                    return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Failure("Target must be a creature for save spells.");

                var dc = caster.Spellcasting?.SpellSaveDC ?? 10;
                var saveRoll = targetCreature.Checks.RollSavingThrow(SaveAbility.Value);
                if (!saveRoll.IsSuccess) return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Failure("Failed to roll save.");

                saveSucceeded = saveRoll.Value >= dc;
                message += $" Target {(saveSucceeded.Value ? "succeeded" : "failed")} {SaveAbility} save (DC {dc}).";
            }

            // 3. Damage
            if (!string.IsNullOrWhiteSpace(DamageDice) && DamageType.HasValue)
            {
                var damageRoll = _diceRoller.Roll(DamageDice);
                if (damageRoll.IsSuccess)
                {
                    int damage = damageRoll.Value.Total;
                    
                    // Crit double dice? (Simplified: just double damage for now or ignore)
                    if (attackResult?.IsCritical == true) damage *= 2;

                    // Save for half?
                    if (SaveAbility.HasValue && saveSucceeded == true) damage /= 2;

                    damageDealt = damage;
                    
                    // Apply damage if target is creature
                    if (target is ICreature targetCreature)
                    {
                        targetCreature.HitPoints.TakeDamage(damage, DamageType.Value);
                        message += $" Dealt {damage} {DamageType} damage.";
                    }
                    
                    if (attackResult != null)
                    {
                        attackResult.AddDamage(new OpenCombatEngine.Core.Models.Combat.DamageRoll(damage, DamageType.Value));
                    }
                }
            }

            return Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(
                new OpenCombatEngine.Core.Models.Spells.SpellResolution(true, message, attackResult, saveSucceeded, damageDealt));
        }
    }
}
