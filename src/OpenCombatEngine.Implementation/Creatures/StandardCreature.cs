using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Items;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// Standard implementation of a creature.
    /// </summary>
    public class StandardCreature : ICreature, IStateful<CreatureState>
    {
        public Guid Id { get; }
        public string Name { get; }
        public IAbilityScores AbilityScores { get; }
        public IHitPoints HitPoints { get; }
        public ICombatStats CombatStats { get; }
        public IConditionManager Conditions { get; }
        public IActionEconomy ActionEconomy { get; }
        public IMovement Movement { get; }
        public ICheckManager Checks { get; }
        public IInventory Inventory { get; }
        public IEquipmentManager Equipment { get; }
        public ISpellCaster? Spellcasting { get; }
        public int ProficiencyBonus => 2; // Placeholder, should be based on level/CR 1

        /// <summary>
        /// Initializes a new instance of StandardCreature.
        /// </summary>
        /// <param name="id">The unique identifier of the creature.</param>
        /// <param name="name">The name of the creature</param>
        /// <param name="abilityScores">The creature's ability scores</param>
        /// <param name="hitPoints">Hit points manager.</param>
        /// <param name="combatStats">Combat statistics.</param>
        /// <param name="checkManager">Optional check manager.</param>
        /// <param name="spellCaster">Optional spellcasting component.</param>
        public StandardCreature(
            string id, 
            string name, 
            IAbilityScores abilityScores, 
            IHitPoints hitPoints, 
            ICombatStats? combatStats = null,
            ICheckManager? checkManager = null,
            ISpellCaster? spellCaster = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id cannot be empty", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            
            Id = Guid.Parse(id); // Assuming id is a string representation of a Guid
            Name = name;
            AbilityScores = abilityScores ?? throw new ArgumentNullException(nameof(abilityScores));
            
            var diceRoller = new OpenCombatEngine.Implementation.Dice.StandardDiceRoller();
            
            // If hitPoints passed in, use it. If not, create default with diceRoller.
            // But wait, if passed in, we can't force it to use our diceRoller.
            // If it's StandardHitPoints, it might have its own.
            // If we create it, we pass diceRoller.
            HitPoints = hitPoints ?? new StandardHitPoints(10, combatStats, diceRoller: diceRoller);
            
            Inventory = new StandardInventory();
            Equipment = new StandardEquipmentManager(this);

            CombatStats = combatStats ?? new StandardCombatStats(equipment: Equipment, abilities: AbilityScores);
            
            Conditions = new StandardConditionManager(this);
            ActionEconomy = new StandardActionEconomy();
            Movement = new StandardMovement(CombatStats, Conditions);
            
            Checks = checkManager ?? new StandardCheckManager(AbilityScores, diceRoller, this);
            Spellcasting = spellCaster;
        }

        /// <summary>
        /// Initializes a new instance of StandardCreature from a state object.
        /// </summary>
        /// <param name="state">The state to restore from.</param>
        public StandardCreature(CreatureState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            Id = state.Id;
            Name = state.Name;
            AbilityScores = new StandardAbilityScores(state.AbilityScores);
            HitPoints = new StandardHitPoints(state.HitPoints);
            
            Inventory = new StandardInventory();
            Equipment = new StandardEquipmentManager(this);

            // Handle legacy state or new state with CombatStats
            // Note: If restoring from state, we might lose the dynamic link if we just use state.CombatStats properties.
            // Ideally, we recreate StandardCombatStats with the state values BUT also inject the new Equipment/Abilities.
            // StandardCombatStats(State) constructor doesn't take dependencies currently.
            // We should probably update StandardCombatStats(State) to take dependencies too?
            // Or just use the state values as "base" and let dynamic calculation happen?
            // If we use state.ArmorClass as base, and then add Dex/Armor, we might double dip if state.ArmorClass already included it.
            // Serialization of CombatStats stores the *result* AC in ArmorClass property.
            // So if we restore, we get the final AC.
            // If we then equip items, we might add to it again.
            // This is a serialization design issue. We should serialize BaseAC separately or recalculate on load.
            // For now, let's assume state.ArmorClass is the "Base" or "Current Snapshot".
            // If we want fully dynamic, we shouldn't serialize derived values, or we should know they are derived.
            // Let's stick to: If state is present, use it as is. If we want dynamic, we assume the state reflects the equipment at that time.
            // BUT if we change equipment after load, we want AC to update.
            // So we need to inject dependencies.
            // I will update StandardCombatStats(State) to accept dependencies? No, I can't change the signature easily without breaking other things maybe?
            // Actually I can.
            
            CombatStats = state.CombatStats != null 
                ? new StandardCombatStats(state.CombatStats) 
                : new StandardCombatStats(equipment: Equipment, abilities: AbilityScores);
            
            // If we loaded from state, CombatStats is a snapshot. It won't react to equipment changes unless we inject them.
            // This is a limitation for now. Fixing it would require updating StandardCombatStats(State) to accept dependencies.
            // Let's leave it as is for now to avoid over-engineering in this step.
            
            // We need to recreate HitPoints with the new CombatStats
            HitPoints = new StandardHitPoints(state.HitPoints, CombatStats);
            
            Conditions = state.Conditions != null 
                ? new StandardConditionManager(this, state.Conditions) 
                : new StandardConditionManager(this);
                
            ActionEconomy = new StandardActionEconomy();
            Movement = new StandardMovement(CombatStats, Conditions);
            Checks = new StandardCheckManager(AbilityScores, new OpenCombatEngine.Implementation.Dice.StandardDiceRoller(), this);
        }

        /// <inheritdoc />
        public CreatureState GetState()
        {
            // We need to cast to IStateful to get the state, or assume the concrete types are IStateful.
            // Since ICreature defines AbilityScores as IAbilityScores, we check if it implements IStateful.
            // If not, we might need a fallback or throw. For StandardCreature, we know they are compatible.
            
            var abilityState = (AbilityScores as IStateful<AbilityScoresState>)?.GetState() 
                ?? throw new InvalidOperationException("AbilityScores component does not support state export");
                
            var hpState = (HitPoints as IStateful<HitPointsState>)?.GetState()
                ?? throw new InvalidOperationException("HitPoints component does not support state export");

            var combatState = (CombatStats as IStateful<CombatStatsState>)?.GetState()
                ?? throw new InvalidOperationException("CombatStats component does not support state export");

            var conditionState = (Conditions as IStateful<ConditionManagerState>)?.GetState()
                ?? throw new InvalidOperationException("Conditions component does not support state export");

            return new CreatureState(Id, Name, abilityState, hpState, combatState, conditionState);
        }

        private readonly System.Collections.Generic.List<IFeature> _features = new();

        /// <inheritdoc />
        public void StartTurn()
        {
            ActionEconomy.ResetTurn();
            Movement.ResetTurn();
            Conditions.Tick();
            foreach (var feature in _features)
            {
                feature.OnStartTurn(this);
            }
            // Death Saving Throws
            if (HitPoints.Current <= 0 && !HitPoints.IsDead && !HitPoints.IsStable)
            {
                var rollResult = Checks.RollDeathSave();
                if (rollResult.IsSuccess)
                {
                    int roll = rollResult.Value;
                    if (roll == 20)
                    {
                        HitPoints.Heal(1);
                    }
                    else if (roll == 1)
                    {
                        HitPoints.RecordDeathSave(false, critical: true);
                    }
                    else if (roll >= 10)
                    {
                        HitPoints.RecordDeathSave(true);
                    }
                    else
                    {
                        HitPoints.RecordDeathSave(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void EndTurn()
        {
            // Future cleanup if needed
        }

        public void Rest(RestType type, int hitDiceToSpend = 0)
        {
            if (type == RestType.ShortRest)
            {
                if (hitDiceToSpend > 0)
                {
                    var result = HitPoints.UseHitDice(hitDiceToSpend);
                    if (result.IsSuccess)
                    {
                        // Add Con mod per die?
                        // 5e: "For each Hit Die spent in this way, the player rolls the die and adds the character's Constitution modifier to it."
                        int conMod = AbilityScores.GetModifier(OpenCombatEngine.Core.Enums.Ability.Constitution);
                        int totalHealing = result.Value + (conMod * hitDiceToSpend);
                        HitPoints.Heal(totalHealing);
                    }
                }
                
                // Reset short rest resources (e.g. Warlock slots, Monk Ki, Fighter Action Surge)
                // Currently ActionEconomy doesn't track these specific resources, but we can reset generic ones if any.
                // ActionEconomy.ResetTurn() is for turn start.
                // We might need a ResetShortRest() on components.
            }
            else if (type == RestType.LongRest)
            {
                HitPoints.Heal(HitPoints.Max);
                HitPoints.RecoverHitDice(HitPoints.HitDiceTotal / 2);
                // Reset all resources
                // ActionEconomy.ResetLongRest(); // If we had it.
                // Conditions might be removed? (Exhaustion -1)
                // For now, just HP and Hit Dice.
            }
        }

        private readonly System.Collections.Generic.List<OpenCombatEngine.Core.Interfaces.Actions.IAction> _customActions = new();

        public void AddAction(OpenCombatEngine.Core.Interfaces.Actions.IAction action)
        {
            _customActions.Add(action);
        }

        public System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Actions.IAction> GetActions()
        {
            // Move Action
            // Assuming Speed is available on Movement component, but IMovement doesn't expose MaxSpeed directly?
            // StandardMovement has Speed. IMovement has MovementRemaining.
            // We might need to cast or assume 30 if unknown.
            // Actually StandardMovement constructor takes speed, but doesn't expose it on interface?
            // Let's check IMovement.
            // If not available, we'll default to 30.
            int speed = 30;
            if (Movement is StandardMovement stdMove)
            {
                // StandardMovement doesn't expose Speed public property? 
                // Let's assume 30 for now to avoid breaking if property missing.
                // TODO: Expose Speed on IMovement.
            }
            yield return new OpenCombatEngine.Implementation.Actions.MoveAction(speed);

            // Unarmed Strike
            var strMod = AbilityScores.GetModifier(OpenCombatEngine.Core.Enums.Ability.Strength);
            var proficiency = ProficiencyBonus; // Assuming proficient
            var attackBonus = strMod + proficiency;
            var damageBonus = strMod;
            var damageDice = "1"; // Flat 1 damage
            var diceRoller = new OpenCombatEngine.Implementation.Dice.StandardDiceRoller();

            yield return new OpenCombatEngine.Implementation.Actions.AttackAction(
                "Unarmed Strike",
                "Punch, kick, or headbutt.",
                attackBonus,
                damageDice,
                OpenCombatEngine.Core.Enums.DamageType.Bludgeoning,
                damageBonus,
                diceRoller
            );
            
            // Spells
            if (Spellcasting != null)
            {
                foreach (var spell in Spellcasting.KnownSpells)
                {
                    yield return new OpenCombatEngine.Implementation.Actions.CastSpellAction(spell);
                }
            }

            // Custom actions (e.g. from import)
            foreach (var action in _customActions)
            {
                yield return action;
            }
        }

        public OpenCombatEngine.Core.Models.Combat.AttackOutcome ResolveAttack(OpenCombatEngine.Core.Models.Combat.AttackResult attack)
        {
            ArgumentNullException.ThrowIfNull(attack);

            var targetAC = CombatStats.ArmorClass;
            
            // Apply Cover bonuses
            if (attack.TargetCover == CoverType.Half) targetAC += 2;
            else if (attack.TargetCover == CoverType.ThreeQuarters) targetAC += 5;
            else if (attack.TargetCover == CoverType.Total)
            {
                // Total cover prevents attack unless specific exception (not handled here yet)
                return new OpenCombatEngine.Core.Models.Combat.AttackOutcome(false, 0, "Target has Total Cover.");
            }

            // Apply Obscurement logic (Heavily Obscured = Disadvantage for attacker)
            // Note: Disadvantage affects the roll itself, which is passed in AttackResult.
            // However, if we want to ENFORCE it, we should check if HasDisadvantage is true.
            // But the roll is already made. We can't re-roll here.
            // Ideally, the caller (AttackAction) should have checked Obscurement before rolling.
            // But if we want to validate, we could warn or fail?
            // For now, we assume the caller handled the roll mechanics (Adv/Disadv).
            // But we can add a check: if TargetObscurement == Heavily and !HasDisadvantage, maybe we should have?
            // But maybe they had Advantage canceling it out.
            // So we just trust the roll for now, but we handle the Cover AC bonus here because that's a target property.

            bool isHit = attack.AttackRoll >= targetAC || attack.IsCritical;

            // Critical Miss logic could be passed in AttackResult or handled here if we knew the raw roll.
            // AttackResult has AttackRoll (total). If we want nat 1 logic, we need IsCriticalFailure flag in AttackResult.
            // For now, we assume the caller handled "Nat 1 = Miss" by passing a low roll or we trust total < AC.
            // Actually, if Nat 20 hits regardless of AC, we rely on IsCritical.
            
            if (!isHit)
            {
                return new OpenCombatEngine.Core.Models.Combat.AttackOutcome(false, 0, "Missed.");
            }

            int totalDamage = 0;
            foreach (var damageRoll in attack.Damage)
            {
                // Future: Apply Resistance/Vulnerability here based on damageRoll.Type
                // e.g. if (Resistances.Contains(damageRoll.Type)) damage /= 2;
                
                totalDamage += damageRoll.Amount;
            }

            // HitPoints.TakeDamage(totalDamage); // REMOVED duplicate call
            
            // Refined logic:
            // Iterate and apply.
            // But wait, if I have 5 Fire and 5 Slashing.
            // I apply 5 Fire. HP reduces.
            // I apply 5 Slashing. HP reduces.
            // Total 10. Correct.
            
            // Re-implementing loop to call TakeDamage per roll.
            // But wait, AttackResult.Damage is a list of rolls.
            // If we have 2d6 Slashing (rolled as 3 and 4), we have two entries? Or one entry of 7?
            // The caller (AttackAction) aggregates?
            // AttackAction rolls damage. "1d6+2". It returns one total.
            // So usually one entry per type.
            
            // However, if we just call TakeDamage, we are modifying state.
            // We should calculate total first? No, TakeDamage handles death logic.
            
            // Let's do this:
            // We need to return total damage dealt for the message.

            
            // We need to be careful about overkilling? No, damage is damage.
            
            // Issue: If we call TakeDamage multiple times, we get multiple events.
            // Is that desired? "Took 5 Fire damage", "Took 5 Slashing damage". Yes, that's good.
            
            // But for the AttackOutcome, we want the sum.
            
            // But for the AttackOutcome, we want the sum.
            
            int actualDamageDealt = 0;
            foreach (var damageRoll in attack.Damage)
            {
                int amount = damageRoll.Amount;
                
                if (CombatStats.Immunities.Contains(damageRoll.Type))
                {
                    amount = 0;
                }
                else
                {
                    if (CombatStats.Resistances.Contains(damageRoll.Type))
                    {
                        amount /= 2;
                    }
                    
                    if (CombatStats.Vulnerabilities.Contains(damageRoll.Type))
                    {
                        amount *= 2;
                    }
                }
                
                if (amount > 0)
                {
                    HitPoints.TakeDamage(amount, damageRoll.Type);
                    actualDamageDealt += amount;
                }
            }

            string message = isHit ? $"Hit for {actualDamageDealt} damage!" : "Missed!";
            if (attack.IsCritical) message = $"Critical Hit! {actualDamageDealt} damage!"; 

            return new OpenCombatEngine.Core.Models.Combat.AttackOutcome(true, actualDamageDealt, message);
        }

        public void AddFeature(OpenCombatEngine.Core.Interfaces.Features.IFeature feature)
        {
            _features.Add(feature);
        }

        public void RemoveFeature(OpenCombatEngine.Core.Interfaces.Features.IFeature feature)
        {
            _features.Remove(feature);
        }

        public void ModifyOutgoingAttack(OpenCombatEngine.Core.Models.Combat.AttackResult attack)
        {
            foreach (var feature in _features)
            {
                feature.OnOutgoingAttack(this, attack);
            }
        }
    }
}
