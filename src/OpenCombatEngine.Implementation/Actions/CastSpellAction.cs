using System;
using System.Globalization;
using System.Linq;
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

            if (IncapacitationCheck.BlockingCondition(source) is { } blockingCondition)
            {
                return Result<ActionResult>.Failure($"{source.Name} is {blockingCondition} and cannot act.");
            }

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

            // Check Action Economy
            if (source.ActionEconomy != null && !context.BypassActionEconomy)
            {
                bool canAct = Type switch
                {
                    ActionType.Action => source.ActionEconomy.HasAction,
                    ActionType.BonusAction => source.ActionEconomy.HasBonusAction,
                    ActionType.Reaction => source.ActionEconomy.HasReaction,
                    _ => true
                };

                if (!canAct)
                {
                    return Result<ActionResult>.Failure($"Cannot perform {Type}: Resource already used.");
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

            // Consume the Action Economy resource now that the cast is committed.
            if (source.ActionEconomy != null && !context.BypassActionEconomy)
            {
                switch (Type)
                {
                    case ActionType.Action:
                        source.ActionEconomy.UseAction();
                        break;
                    case ActionType.BonusAction:
                        source.ActionEconomy.UseBonusAction();
                        break;
                    case ActionType.Reaction:
                        source.ActionEconomy.UseReaction();
                        break;
                }
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
                // Range and LOS Check
                if (context.Grid != null && creatureTarget != null)
                {
                    var sourcePos = context.Grid.GetPosition(source);
                    var targetPos = context.Grid.GetPosition(creatureTarget);

                    if (sourcePos != null && targetPos != null)
                    {
                        var rangeFeet = ParseRangeInFeet(_spell.Range);
                        if (rangeFeet.HasValue && rangeFeet.Value > 0)
                        {
                            var distance = context.Grid.GetDistance(sourcePos.Value, targetPos.Value);
                            if (distance > rangeFeet.Value)
                            {
                                return Result<ActionResult>.Failure($"Target is out of range. Distance: {distance}, Range: {rangeFeet.Value}");
                            }
                        }

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

        /// <summary>
        /// Parses an SRD spell range string ("60 feet", "Touch", "Self")
        /// into feet, or <see langword="null"/> when it isn't a
        /// parseable numeric/Touch/Self range (e.g. "Sight",
        /// "Unlimited"). "Self" is 0 feet, "Touch" is 5 feet (this
        /// engine's grid convention — same 5-ft-square SRD assumption
        /// <c>StandardGridManager.GetDistance</c> already uses). Public
        /// so callers outside this class (e.g. the gRPC sidecar's
        /// <c>GetAvailableActions</c> handler) can determine whether a
        /// prepared spell would be in range of a candidate target
        /// without duplicating this parsing.
        /// </summary>
        public static int? ParseRangeInFeet(string range)
        {
            if (string.IsNullOrWhiteSpace(range)) return null;
            if (range.Equals("Self", StringComparison.OrdinalIgnoreCase)) return 0;
            if (range.Equals("Touch", StringComparison.OrdinalIgnoreCase)) return 5;

            var digits = new string(range.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int feet)
                ? feet
                : null;
        }

        /// <summary>
        /// Rolls a single spell attack (1d20 + proficiency + casting
        /// ability modifier, same formula ApplySpellEffects already uses
        /// for save DC) against target's own armor class, appends a
        /// human-readable line to messages, and returns whether it hit.
        /// A natural 20 always hits and a natural 1 always misses, same as
        /// any other d20 attack roll; this doesn't model a critical hit's
        /// extra damage dice, a further known simplification. If the
        /// attack roll itself fails to resolve, treats it as a hit rather
        /// than silently losing the cast's already-committed resources.
        /// </summary>
        private bool RollSpellAttack(ICreature source, ICreature target, System.Collections.Generic.List<string> messages)
        {
            int attackBonus = 0;
            if (source.Spellcasting is OpenCombatEngine.Implementation.Spells.StandardSpellCaster ssc)
            {
                int prof = source.ProficiencyBonus;
                int mod = source.AbilityScores.GetModifier(ssc.CastingAbility);
                attackBonus = prof + mod;
            }

            var attackRoll = _diceRoller.Roll("1d20");
            if (!attackRoll.IsSuccess)
            {
                return true;
            }

            int natural = attackRoll.Value.Total;
            int targetAc = target.CombatStats.ArmorClass;
            bool hit = natural == 20 || (natural != 1 && natural + attackBonus >= targetAc);

            messages.Add(hit
                ? $"Attack roll {natural}+{attackBonus} hits AC {targetAc}."
                : $"Attack roll {natural}+{attackBonus} misses AC {targetAc}.");
            return hit;
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
                    saveSuccess = saveResult.Value.Total >= dc;
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

                    // Cantrip Scaling (character level) or fixed
                    // multi-instance scaling (e.g. Magic Missile's three
                    // darts, Scorching Ray's three rays, each independently
                    // rolled — SRD "you create three ... darts/rays"). A
                    // spell uses at most one of these two mechanisms in
                    // practice: cantrip scaling only applies at Level 0,
                    // where InstanceCount is always the SRD default of 1.
                    int multiplier = 1;
                    if (_spell.Level == 0 && source.LevelManager != null)
                    {
                        int level = source.LevelManager.TotalLevel;
                        if (level >= 17) multiplier = 4;
                        else if (level >= 11) multiplier = 3;
                        else if (level >= 5) multiplier = 2;
                    }
                    else if (_spell.InstanceCount > 1 || _spell.InstanceCountPerUpcastLevel > 0)
                    {
                        int extraLevels = System.Math.Max(0, _slotLevel - _spell.Level);
                        multiplier = _spell.InstanceCount + (_spell.InstanceCountPerUpcastLevel * extraLevels);
                    }

                    for (int i = 0; i < multiplier; i++)
                    {
                        // Each instance (dart/ray/...) rolls its own
                        // attack, if the spell requires one — a miss deals
                        // no damage for that instance specifically, not
                        // the whole cast (SRD: Scorching Ray's rays each
                        // roll their own attack). A save-based spell rolls
                        // its single save once, above, outside this loop —
                        // SRD spells use an attack roll or a save, never
                        // both.
                        if (_spell.RequiresAttackRoll && !_spell.SaveAbility.HasValue && !RollSpellAttack(source, target, messages))
                        {
                            continue;
                        }

                        foreach (var rollDef in _spell.DamageRolls)
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
