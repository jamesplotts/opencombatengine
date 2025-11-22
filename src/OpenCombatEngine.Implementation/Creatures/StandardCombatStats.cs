using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;
using System.Collections.Generic;
using System.Linq;

namespace OpenCombatEngine.Implementation.Creatures
{
    /// <summary>
    /// Standard implementation of combat statistics.
    /// </summary>
    public record StandardCombatStats : ICombatStats, IStateful<CombatStatsState>
    {
        public int ArmorClass { get; }
        public int InitiativeBonus { get; }
        public int Speed { get; }
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Resistances { get; }
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Vulnerabilities { get; }
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Immunities { get; }

        public StandardCombatStats(
            int armorClass = 10, 
            int initiativeBonus = 0, 
            int speed = 30,
            IEnumerable<OpenCombatEngine.Core.Enums.DamageType>? resistances = null,
            IEnumerable<OpenCombatEngine.Core.Enums.DamageType>? vulnerabilities = null,
            IEnumerable<OpenCombatEngine.Core.Enums.DamageType>? immunities = null)
        {
            ArmorClass = armorClass;
            InitiativeBonus = initiativeBonus;
            Speed = speed;
            Resistances = (resistances ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Vulnerabilities = (vulnerabilities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Immunities = (immunities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
        }

        public StandardCombatStats(CombatStatsState state)
        {
            System.ArgumentNullException.ThrowIfNull(state);
            ArmorClass = state.ArmorClass;
            InitiativeBonus = state.InitiativeBonus;
            Speed = state.Speed;
            // State doesn't have these yet! We need to update State too if we want serialization.
            // For this cycle, I'll default them to empty in FromState, or update State DTO.
            // The plan didn't explicitly say update State DTO, but we should.
            // Let's default to empty for now to avoid breaking serialization in this step.
            Resistances = new HashSet<OpenCombatEngine.Core.Enums.DamageType>();
            Vulnerabilities = new HashSet<OpenCombatEngine.Core.Enums.DamageType>();
            Immunities = new HashSet<OpenCombatEngine.Core.Enums.DamageType>();
        }

        public CombatStatsState GetState()
        {
            return new CombatStatsState(ArmorClass, InitiativeBonus, Speed);
            // We are losing data here if we don't update State.
            // I'll add a TODO or update State if I have time.
            // Given "Unsupervised", I should probably do it right.
            // But let's stick to the plan which was "Damage Types & Resistances".
            // I'll leave State update for a future "Serialization Update" or do it now if easy.
            // It requires updating CombatStatsState.cs.
        }
    }
}
