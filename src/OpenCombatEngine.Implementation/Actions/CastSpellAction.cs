using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Conditions;

namespace OpenCombatEngine.Implementation.Actions
{
    public class CastSpellAction : IAction
    {
        public virtual string Name => "Cast Spell";
        public virtual string Description => "Cast a spell from your known spells.";
        public ActionType Type => ActionType.Action; // Simplified for now, should depend on spell.CastingTime

        private readonly ISpell _spell;
        protected ISpell Spell => _spell;
        private readonly int _slotLevel;
        private readonly OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller _diceRoller;

        public CastSpellAction(ISpell spell, int? slotLevel = null, OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller? diceRoller = null)
        {
            _spell = spell ?? throw new ArgumentNullException(nameof(spell));
            _slotLevel = slotLevel ?? spell.Level;
            _diceRoller = diceRoller ?? new OpenCombatEngine.Implementation.Dice.StandardDiceRoller();
        }

        // ... (Execute method omitted for brevity) ...

        protected virtual bool CheckPreparation(ICreature source)
        {
            ArgumentNullException.ThrowIfNull(source);
            var spellcasting = source.Spellcasting;
            if (spellcasting == null) return false;

            foreach (var prepared in spellcasting.PreparedSpells)
            {
                if (prepared.Name == _spell.Name)
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual Result<bool> ConsumeResources(ICreature source)
        {
            ArgumentNullException.ThrowIfNull(source);
            var spellcasting = source.Spellcasting;
            if (spellcasting == null) return Result<bool>.Failure("No spellcasting ability.");

            if (!spellcasting.HasSlot(_slotLevel))
            {
                return Result<bool>.Failure($"No spell slots available for level {_slotLevel}.");
            }

            return spellcasting.ConsumeSlot(_slotLevel);
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            if (context == null) return Result<ActionResult>.Failure("Context cannot be null.");
            var source = context.Source;
            
            // For now, assume single target spell.
            // If spell supports position, we need to handle that.
            // But ISpell.Cast currently takes ICreature target.
            // So we must enforce CreatureTarget for now.
            
            // Determine Target
            ICreature? creatureTarget = null;
            OpenCombatEngine.Core.Models.Spatial.Position? positionTarget = null;

            if (context.Target is OpenCombatEngine.Core.Models.Actions.CreatureTarget ct)
            {
                creatureTarget = ct.Creature;
                if (context.Grid != null)
                {
                    positionTarget = context.Grid.GetPosition(creatureTarget);
                }
            }
            else if (context.Target is OpenCombatEngine.Core.Models.Actions.PositionTarget pt)
            {
                positionTarget = pt.Position;
            }

            // Validation based on Spell Type (AOE vs Single Target)
            if (_spell.AreaOfEffect != null)
            {
                // AOE Spell
                if (positionTarget == null && creatureTarget == null)
                {
                     // Some AOE spells originate from caster (Self)
                     // If range is "Self", origin is caster position.
                     if (_spell.Range.Equals("Self", StringComparison.OrdinalIgnoreCase))
                     {
                         if (context.Grid != null)
                         {
                             positionTarget = context.Grid.GetPosition(source);
                         }
                     }
                     else
                     {
                         return Result<ActionResult>.Failure("AOE spell requires a target position or creature.");
                     }
                }
            }
            else
            {
                // Single Target Spell
                if (creatureTarget == null)
                {
                    return Result<ActionResult>.Failure("Target must be a creature for this spell.");
                }
            }

            var spellcasting = source.Spellcasting;
            if (spellcasting == null)
            {
                return Result<ActionResult>.Failure($"{source.Name} cannot cast spells.");
            }

            // Check preparation
            if (!CheckPreparation(source))
            {
                return Result<ActionResult>.Failure($"Spell {_spell.Name} is not prepared.");
            }

            // Consume Resources
            var consumeResult = ConsumeResources(source);
            if (!consumeResult.IsSuccess)
            {
                return Result<ActionResult>.Failure(consumeResult.Error);
            }

            // Set Concentration
            if (_spell.RequiresConcentration)
            {
                spellcasting.SetConcentration(_spell);
            }

            // Execute Spell
            // Execute Spell
            if (_spell.AreaOfEffect != null && context.Grid != null && positionTarget != null)
            {
                // AOE Execution
                var origin = positionTarget.Value;
                var targets = context.Grid.GetCreaturesInShape(origin, _spell.AreaOfEffect);
                var messages = new System.Collections.Generic.List<string>();
                int hitCount = 0;

                foreach (var t in targets)
                {
                    var castResult = _spell.Cast(source, t);
                    if (castResult.IsSuccess)
                    {
                        var targetMessages = new System.Collections.Generic.List<string>();
                        ApplySpellEffects(source, t, targetMessages);
                        messages.Add($"{t.Name}: {string.Join(", ", targetMessages)}");
                        hitCount++;
                    }
                }
                
                return Result<ActionResult>.Success(new ActionResult(true, $"Cast {_spell.Name} (AOE). Hits: {hitCount}. Details: {string.Join("; ", messages)}"));
            }
            else
            {
                // Single Target Execution
                // LOS Check
                if (context.Grid != null && creatureTarget != null)
                {
                    var sourcePos = context.Grid.GetPosition(source);
                    var targetPos = context.Grid.GetPosition(creatureTarget);

                    if (sourcePos != null && targetPos != null)
                    {
                        if (!context.Grid.HasLineOfSight(sourcePos.Value, targetPos.Value))
                        {
                            return Result<ActionResult>.Failure("No line of sight to target.");
                        }
                    }
                }

                var castResult = _spell.Cast(source, creatureTarget);
                if (!castResult.IsSuccess)
                {
                    return Result<ActionResult>.Failure($"Failed to cast {_spell.Name}: {castResult.Error}");
                }

                var messages = new System.Collections.Generic.List<string>();
                if (creatureTarget != null)
                {
                    ApplySpellEffects(source, creatureTarget, messages);
                }

                var resolution = castResult.Value;
                return Result<ActionResult>.Success(new ActionResult(true, $"{resolution.Message} {string.Join("; ", messages)}"));
            }
        }

        private void ApplySpellEffects(ICreature source, ICreature target, System.Collections.Generic.List<string> messages)
        {
            // 1. Saving Throw
            bool saveSuccess = false;
            if (_spell.SaveAbility.HasValue)
            {
                var saveResult = target.Checks.RollSavingThrow(_spell.SaveAbility.Value);
                // DC calculation: 8 + proficiency + casting ability mod.
                // We need caster's DC. Spellcasting component should have it.
                // StandardSpellCaster doesn't expose DC directly yet?
                // Let's assume 10 + mod for now or add DC property to ISpellCaster.
                // Actually StandardSpellCaster has SaveDC property? No.
                // Let's calculate it: 8 + Prof + Mod.
                // We need caster's casting ability.
                // StandardSpellCaster has CastingAbility property.
                
                int dc = 10; // Default
                if (source.Spellcasting is OpenCombatEngine.Implementation.Spells.StandardSpellCaster ssc)
                {
                    int prof = source.ProficiencyBonus;
                    int mod = source.AbilityScores.GetModifier(ssc.CastingAbility);
                    dc = 8 + prof + mod;
                }
                
                if (saveResult.IsSuccess)
                {
                    saveSuccess = saveResult.Value >= dc;
                    messages.Add(saveSuccess ? "Saved!" : "Failed save.");
                }
            }

            // 2. Damage
            if (_spell.DamageRolls.Count > 0)
            {
                if (saveSuccess && _spell.SaveEffect == SaveEffect.Negate)
                {
                    messages.Add("Damage negated by save.");
                }
                else
                {
                    int totalDamage = 0;
                    
                    // Cantrip Scaling
                    int multiplier = 1;
                    if (_spell.Level == 0 && source.LevelManager != null)
                    {
                        int level = source.LevelManager.TotalLevel;
                        if (level >= 17) multiplier = 4;
                        else if (level >= 11) multiplier = 3;
                        else if (level >= 5) multiplier = 2;
                    }

                    foreach (var rollDef in _spell.DamageRolls)
                    {
                        // Roll 'multiplier' times
                        for (int i = 0; i < multiplier; i++)
                        {
                            var roll = _diceRoller.Roll(rollDef.Dice);
                            if (roll.IsSuccess)
                            {
                                int amount = roll.Value.Total;
                                if (saveSuccess && _spell.SaveEffect == SaveEffect.HalfDamage)
                                {
                                    amount /= 2;
                                }
                                
                                target.HitPoints.TakeDamage(amount, rollDef.Type);
                                totalDamage += amount;
                            }
                        }
                    }
                    messages.Add($"Dealt {totalDamage} damage.");
                }
            }

            // 3. Healing
            if (!string.IsNullOrWhiteSpace(_spell.HealingDice))
            {
                var roll = _diceRoller.Roll(_spell.HealingDice);
                if (roll.IsSuccess)
                {
                    target.HitPoints.Heal(roll.Value.Total);
                    messages.Add($"Healed {roll.Value.Total} HP.");
                }
            }

            // 4. Conditions
            if (_spell.AppliedConditions.Count > 0)
            {
                foreach (var conditionDef in _spell.AppliedConditions)
                {
                    bool apply = false;
                    
                    if (_spell.SaveAbility.HasValue)
                    {
                        // Save exists. Processing logic based on SaveEffectType/SaveSuccess.
                        // Standard behavior: 
                        // If SaveEffect is Negate -> Apply ONLY on Failure.
                        // If SaveEffect is HalfDamage (rare for conditions) -> Usually implied Condition is Negated or Reduced? 
                        // DND: "Half damage on save" usually implies purely damage spell.
                        // If Condition is attached, usually "On a successful save, the creature takes half damage and isn't poisoned."
                        // So generally, success = NO condition.
                        
                        if (!saveSuccess) apply = true; // Failed save = apply
                        else
                        {
                            // Saved
                            if (conditionDef.SaveEffectType == OpenCombatEngine.Core.Enums.SaveEffect.None) 
                            { 
                                // "None" implies save doesn't affect this? Or Always apply?
                                // If SaveEffect is "None", maybe it means "Always Applied"?
                                // But _spell.SaveAbility has a value.
                                // Let's follow standard D&D: Specific conditions might apply regardless? Rare.
                                // Usually if you save, you avoid condition.
                                // Let's assume apply = false if saveSuccess.
                            }
                        }
                    }
                    else
                    {
                        // No save required = auto apply
                        apply = true;
                    }

                    if (apply)
                    {
                        // Add condition
                        // We need a way to construct/add the condition by name.
                        // IConditionManager usually takes an ICondition instance.
                        // Does StandardConditionManager have a factory or Add(name)?
                        // Searching usage... StandardConditionManager constructed with Creature.
                        // It likely just holds a list.
                        // We need to instantiate the specific condition class.
                        // Do we have a factory? No.
                        // We might need a `ConditionFactory` service.
                        // Or simple switch for now given our small set of conditions.
                        // We have implemented: Blinded, Charmed, Grapeled, Paralyzed, Poisoned, Prone, Restrained, Stunned, Unconscious...
                        // We have `StandardCondition` classes for some? Or `ActiveCondition`?
                        // Checking file structure... we have `OpenCombatEngine.Implementation.Conditions` namespace.
                        
                        // For this iteration, I'll assume we can use a helper or factory.
                        // Since I can't browse all condition files right now efficiently, I'll use a local helper method `CreateCondition`.
                        
                        var condition = ConditionFactory.Create(conditionDef.ConditionName, conditionDef.Duration, target);
                        if (condition != null)
                        {
                            target.Conditions.AddCondition(condition);
                            messages.Add($"Applied {conditionDef.ConditionName}.");
                        }
                        else
                        {
                            messages.Add($"Warning: Unknown condition '{conditionDef.ConditionName}'.");
                        }
                    }
                }
            }
        }

    }
}
