using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;

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

        public StandardCombatStats(int armorClass = 10, int initiativeBonus = 0, int speed = 30)
        {
            ArmorClass = armorClass;
            InitiativeBonus = initiativeBonus;
            Speed = speed;
        }

        public StandardCombatStats(CombatStatsState state)
        {
            System.ArgumentNullException.ThrowIfNull(state);
            ArmorClass = state.ArmorClass;
            InitiativeBonus = state.InitiativeBonus;
            Speed = state.Speed;
        }

        public CombatStatsState GetState()
        {
            return new CombatStatsState(ArmorClass, InitiativeBonus, Speed);
        }
    }
}
