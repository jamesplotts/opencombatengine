using System;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Implementation.Conditions;

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
        public int ProficiencyBonus { get; } = 2; // Default for level 1

        /// <summary>
        /// Initializes a new instance of StandardCreature.
        /// </summary>
        /// <param name="id">The unique identifier of the creature.</param>
        /// <param name="name">The name of the creature</param>
        /// <param name="abilityScores">The creature's ability scores</param>
        /// <param name="hitPoints">Hit points manager.</param>
        /// <param name="combatStats">Combat statistics.</param>
        /// <param name="checkManager">Optional check manager.</param>
        public StandardCreature(
            string id, 
            string name, 
            IAbilityScores abilityScores, 
            IHitPoints hitPoints, 
            ICombatStats? combatStats = null,
            ICheckManager? checkManager = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id cannot be empty", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            
            Id = Guid.Parse(id); // Assuming id is a string representation of a Guid
            Name = name;
            AbilityScores = abilityScores ?? throw new ArgumentNullException(nameof(abilityScores));
            HitPoints = hitPoints ?? throw new ArgumentNullException(nameof(hitPoints));
            CombatStats = combatStats ?? new StandardCombatStats();
            
            Conditions = new StandardConditionManager(this);
            ActionEconomy = new StandardActionEconomy();
            Movement = new StandardMovement(CombatStats, Conditions);
            
            Checks = checkManager ?? new StandardCheckManager(AbilityScores, new OpenCombatEngine.Implementation.Dice.StandardDiceRoller(), this);
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
            // Handle legacy state or new state with CombatStats
            CombatStats = state.CombatStats != null 
                ? new StandardCombatStats(state.CombatStats) 
                : new StandardCombatStats();
            
            // We need to recreate HitPoints with the new CombatStats
            // But state.HitPoints is a HitPointsState. StandardHitPoints(state) doesn't take CombatStats.
            // We might need a new constructor on StandardHitPoints that takes State AND CombatStats.
            // Or we just set it? No, it's readonly.
            // Let's add a constructor to StandardHitPoints that takes State and CombatStats.
            // For now, let's just use the main constructor if we can map state?
            // Or update StandardHitPoints(State) to accept optional CombatStats.
            // I'll update StandardHitPoints(State) in a moment.
            // Assuming I update it:
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
    }
}
