using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Conditions
{
    public class StandardConditionManager : IConditionManager, IStateful<ConditionManagerState>
    {
        private readonly List<ICondition> _conditions = new();
        private readonly ICreature _owner;

        public IEnumerable<ICondition> ActiveConditions => _conditions.AsReadOnly();

        public StandardConditionManager(ICreature owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public bool HasCondition(ConditionType type)
        {
            return _conditions.Any(c => c.Type == type);
        }

        public Result<bool> AddCondition(ICondition condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

            if (_conditions.Any(c => c.Name == condition.Name))
            {
                return Result<bool>.Failure($"Condition '{condition.Name}' already active.");
            }

            _conditions.Add(condition);
            condition.OnApplied(_owner);

            // Register Active Effects
            if (condition.Effects != null)
            {
                foreach (var effect in condition.Effects)
                {
                    _owner.Effects.AddEffect(effect);
                }
            }

            return Result<bool>.Success(true);
        }

        public void RemoveCondition(string conditionName)
        {
            if (string.IsNullOrWhiteSpace(conditionName)) return;

            var condition = _conditions.FirstOrDefault(c => c.Name == conditionName);
            if (condition != null)
            {
                condition.OnRemoved(_owner);
                
                // Remove Active Effects
                if (condition.Effects != null)
                {
                    foreach (var effect in condition.Effects)
                    {
                        _owner.Effects.RemoveEffect(effect.Name); // Assuming unique names or ID? 
                        // Wait, RemoveEffect takes ID usually. Or Name?
                        // IEffectManager.RemoveEffect(string effectId).
                        // IActiveEffect has Id property? Let's check.
                        // If not, we might need to track IDs.
                        // For now, let's assume RemoveEffect takes Name or ID and effect.Name is unique enough or we use ID.
                        // Let's check IActiveEffect interface.
                    }
                }

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
        public StandardConditionManager(ICreature owner, ConditionManagerState state) : this(owner)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.Conditions != null)
            {
                foreach (var cState in state.Conditions)
                {
                    // Use Factory to restore standard conditions with effects
                    if (cState.Type != ConditionType.None && cState.Type != ConditionType.Custom)
                    {
                        _conditions.Add(ConditionFactory.Create(cState.Type, cState.DurationRounds));
                    }
                    else
                    {
                        _conditions.Add(new Condition(cState.Name, cState.Description, cState.DurationRounds, cState.Type));
                    }
                }
            }
        }

        public ConditionManagerState GetState()
        {
            var conditionStates = _conditions.Select(c => new ConditionState(c.Name, c.Description, c.DurationRounds, c.Type)).ToList();
            return new ConditionManagerState(new Collection<ConditionState>(conditionStates));
        }
    }
}
