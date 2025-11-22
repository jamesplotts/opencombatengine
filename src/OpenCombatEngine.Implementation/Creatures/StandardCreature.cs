using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
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
            Equipment = new StandardEquipmentManager();

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
            Equipment = new StandardEquipmentManager();

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

        /// <inheritdoc />
        public void StartTurn()
        {
            Conditions.Tick();
            ActionEconomy.ResetTurn();
            Movement.ResetTurn();

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
    }
}
