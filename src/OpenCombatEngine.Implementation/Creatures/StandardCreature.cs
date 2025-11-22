using System;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;

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

        /// <summary>
        /// Initializes a new instance of StandardCreature.
        /// </summary>
        /// <param name="name">The name of the creature</param>
        /// <param name="abilityScores">The creature's ability scores</param>
        /// <param name="hitPoints">The creature's hit points</param>
        /// <param name="combatStats">The creature's combat stats</param>
        /// <param name="id">Optional unique identifier (generates new Guid if null)</param>
        public StandardCreature(
            string name,
            IAbilityScores abilityScores,
            IHitPoints hitPoints,
            ICombatStats? combatStats = null,
            Guid? id = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Creature name cannot be null or empty", nameof(name));

            Id = id ?? Guid.NewGuid();
            Name = name;
            AbilityScores = abilityScores ?? throw new ArgumentNullException(nameof(abilityScores));
            HitPoints = hitPoints ?? throw new ArgumentNullException(nameof(hitPoints));
            CombatStats = combatStats ?? new StandardCombatStats();
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

            return new CreatureState(Id, Name, abilityState, hpState, combatState);
        }
    }
}
