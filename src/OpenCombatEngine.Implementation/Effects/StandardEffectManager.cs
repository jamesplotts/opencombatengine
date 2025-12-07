using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Effects
{
    public class StandardEffectManager : IEffectManager
    {
        private readonly List<IActiveEffect> _effects = new();
        private readonly ICreature _owner;

        public IEnumerable<IActiveEffect> ActiveEffects => _effects.AsReadOnly();

        public StandardEffectManager(ICreature owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public Result<bool> AddEffect(IActiveEffect effect)
        {
            if (effect == null) return Result<bool>.Failure("Effect cannot be null.");
            
            // Check for duplicates? For now, allow multiples (stacking rules can be complex)
            // Or maybe replace existing with same name?
            // Let's assume replace for now if same name.
            var existing = _effects.FirstOrDefault(e => e.Name == effect.Name);
            if (existing != null)
            {
                RemoveEffect(existing.Name);
            }

            _effects.Add(effect);
            effect.OnApplied(_owner);
            return Result<bool>.Success(true);
        }

        public void RemoveEffect(string effectName)
        {
            var effect = _effects.FirstOrDefault(e => e.Name == effectName);
            if (effect != null)
            {
                effect.OnRemoved(_owner);
                _effects.Remove(effect);
            }
        }

        public void Tick()
        {
            // Iterate backwards to allow removal
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.OnTurnStart(_owner);

                // If duration is managed by rounds (Round, Minute, Hour, etc.) and reaches 0, remove.
                // UntilStartOfNextTurn implies 1 round duration, so decrementing to 0 here works.
                // Permanent (-1) is ignored.
                if (effect.DurationRounds == 0 && effect.DurationType != DurationType.Permanent)
                {
                    RemoveEffect(effect.Name);
                }
            }
        }

        public void OnTurnEnd()
        {
            // Iterate backwards to allow removal
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var effect = _effects[i];
                effect.OnTurnEnd(_owner);

                if (effect.DurationType == DurationType.UntilEndOfTurn)
                {
                    RemoveEffect(effect.Name);
                }
            }
        }

        public int ApplyStatBonuses(StatType stat, int baseValue)
        {
            int currentValue = baseValue;
            foreach (var effect in _effects)
            {
                currentValue = effect.ModifyStat(stat, currentValue);
            }
            return currentValue;
        }
    }
}
