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
        private readonly string? _targetSkillName;

        /// <summary>
        /// Creates a stat bonus effect.
        /// </summary>
        /// <param name="name">The effect's unique name (see <see cref="IActiveEffect.Name"/>).</param>
        /// <param name="description">A human-readable description of the effect.</param>
        /// <param name="durationRounds">Remaining duration in rounds; -1 for permanent.</param>
        /// <param name="targetStat">The stat this effect modifies.</param>
        /// <param name="bonus">The amount added to the stat when this effect applies.</param>
        /// <param name="durationType">The duration/expiry rule governing this effect.</param>
        /// <param name="targetSkillName">
        /// When <paramref name="targetStat"/> is <see cref="StatType.AbilityCheck"/>,
        /// scopes this bonus to one named skill (e.g. "Intimidation") instead
        /// of every ability check — compared case-insensitively against the
        /// skill name a check is rolled/probed for. Null (the default)
        /// applies to every check of <paramref name="targetStat"/>, matching
        /// this effect's original whole-stat behavior.
        /// </param>
        public StatBonusEffect(string name, string description, int durationRounds, StatType targetStat, int bonus, DurationType durationType = DurationType.Round, string? targetSkillName = null)
        {
            Name = name;
            Description = description;
            DurationRounds = durationRounds;
            _targetStat = targetStat;
            _bonus = bonus;
            DurationType = durationType;
            _targetSkillName = targetSkillName;
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

        public int ModifyStat(StatType stat, int currentValue, string? skillName = null)
        {
            if (stat != _targetStat)
            {
                return currentValue;
            }
            if (_targetSkillName == null)
            {
                // Whole-stat bonus (e.g. every ability check, not scoped to
                // a specific skill) — this effect's original behavior.
                return currentValue + _bonus;
            }
            if (skillName != null && string.Equals(skillName, _targetSkillName, StringComparison.OrdinalIgnoreCase))
            {
                return currentValue + _bonus;
            }
            return currentValue;
        }
    }
}
