using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Implementation.Conditions
{
    public class Condition : ICondition
    {
        public string Name { get; }
        public string Description { get; }
        public int DurationRounds { get; private set; }
        public ConditionType Type { get; }

        public System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Effects.IActiveEffect> Effects { get; }

        public Condition(string name, string description, int durationRounds, ConditionType type = ConditionType.None, System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Effects.IActiveEffect>? effects = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
            Description = description;
            DurationRounds = durationRounds;
            Type = type;
            Effects = effects ?? System.Linq.Enumerable.Empty<OpenCombatEngine.Core.Interfaces.Effects.IActiveEffect>();
        }

        public virtual void OnApplied(ICreature target) { }
        public virtual void OnRemoved(ICreature target) { }

        public virtual void OnTurnStart(ICreature target)
        {
            if (DurationRounds > 0)
            {
                DurationRounds--;
            }
        }
    }
}
