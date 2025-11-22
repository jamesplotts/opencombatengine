using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Implementation.Conditions
{
    public class StandardConditionManager : IConditionManager
    {
        private readonly List<ICondition> _conditions = new();
        private readonly ICreature _owner;

        public IEnumerable<ICondition> ActiveConditions => _conditions.AsReadOnly();

        public StandardConditionManager(ICreature owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void AddCondition(ICondition condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

            // Check if condition already exists (by name)
            var existing = _conditions.FirstOrDefault(c => c.Name == condition.Name);
            if (existing != null)
            {
                // Logic for stacking or replacing?
                // For now, let's replace/refresh.
                RemoveCondition(existing.Name);
            }

            _conditions.Add(condition);
            condition.OnApplied(_owner);
        }

        public void RemoveCondition(string conditionName)
        {
            if (string.IsNullOrWhiteSpace(conditionName)) return;

            var condition = _conditions.FirstOrDefault(c => c.Name == conditionName);
            if (condition != null)
            {
                condition.OnRemoved(_owner);
                _conditions.Remove(condition);
            }
        }

        public void Tick()
        {
            // Iterate backwards to allow removal during iteration if needed
            for (int i = _conditions.Count - 1; i >= 0; i--)
            {
                var condition = _conditions[i];
                condition.OnTurnStart(_owner);

                // Check duration if not permanent (-1)
                // Note: ICondition interface has DurationRounds property, but it's read-only.
                // The condition implementation itself needs to track its remaining duration and decrement it.
                // Or the manager tracks it? The interface implies the condition knows its duration.
                // If ICondition is immutable/stateless, this doesn't work.
                // Let's assume ICondition is stateful or we need a wrapper.
                // The plan said "Tick() ... decrement durations".
                // If ICondition.DurationRounds is just the *initial* duration, we need a wrapper.
                // But the interface says "Remaining duration". So the implementation must be mutable.
                
                if (condition.DurationRounds == 0)
                {
                    RemoveCondition(condition.Name);
                }
            }
        }
    }
}
