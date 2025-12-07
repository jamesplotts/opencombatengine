using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Effects;

namespace OpenCombatEngine.Implementation.Effects
{
    public class StatBonusEffect : IActiveEffect
    {
        public string Name { get; }
        public string Description { get; }
        public DurationType DurationType { get; }
        public int DurationRounds { get; private set; }
        
        private readonly StatType _targetStat;
        private readonly int _bonus;

        public StatBonusEffect(string name, string description, int durationRounds, StatType targetStat, int bonus, DurationType durationType = DurationType.Round)
        {
            Name = name;
            Description = description;
            DurationRounds = durationRounds;
            _targetStat = targetStat;
            _bonus = bonus;
            DurationType = durationType;
        }

        public void OnApplied(ICreature target)
        {
            // Optional: Log or trigger event
        }

        public void OnRemoved(ICreature target)
        {
            // Optional: Log or trigger event
        }

        public void OnTurnStart(ICreature target)
        {
            if (DurationType == DurationType.Permanent || DurationType == DurationType.UntilEndOfTurn) return;

            if (DurationRounds > 0)
            {
                DurationRounds--;
            }
        }

        public void OnTurnEnd(ICreature target)
        {
            // Logic handled by Manager for UntilEndOfTurn
        }

        public int ModifyStat(StatType stat, int currentValue)
        {
            if (stat == _targetStat)
            {
                return currentValue + _bonus;
            }
            return currentValue;
        }
    }
}
