using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            Resistances = (state.Resistances ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Vulnerabilities = (state.Vulnerabilities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
            Immunities = (state.Immunities ?? Enumerable.Empty<OpenCombatEngine.Core.Enums.DamageType>()).ToHashSet();
        }

        public CombatStatsState GetState()
        {
            return new CombatStatsState(
                ArmorClass, 
                InitiativeBonus, 
                Speed, 
                new Collection<OpenCombatEngine.Core.Enums.DamageType>(Resistances.ToList()), 
                new Collection<OpenCombatEngine.Core.Enums.DamageType>(Vulnerabilities.ToList()), 
                new Collection<OpenCombatEngine.Core.Enums.DamageType>(Immunities.ToList())
            );
        }
    }
}
