using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Features
{
    public class StatBonusFeature : IFeature
    {
        public string Name { get; }
        public string Description { get; }

        private readonly Dictionary<StatType, int> _bonuses;

        public StatBonusFeature(string name, string description, Dictionary<StatType, int> bonuses)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
            Description = description;
            _bonuses = bonuses ?? new Dictionary<StatType, int>();
        }

        private readonly List<string> _appliedEffectNames = new();

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            foreach (var kvp in _bonuses)
            {
                var effectName = $"{Name}_{kvp.Key}";
                var effect = new OpenCombatEngine.Implementation.Effects.StatBonusEffect(
                    effectName,
                    $"Bonus from {Name}",
                    -1, // Permanent
                    kvp.Key,
                    kvp.Value
                );
                
                creature.Effects.AddEffect(effect);
                _appliedEffectNames.Add(effectName);
            }
        }

        public void OnRemoved(ICreature creature)
        {
            if (creature == null) return;

            foreach (var name in _appliedEffectNames)
            {
                creature.Effects.RemoveEffect(name);
            }
            _appliedEffectNames.Clear();
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack)
        {
            // Handled via Effects system
        }

        public void OnStartTurn(ICreature creature)
        {
            // No turn start logic needed for static bonuses
        }

        public int GetStatBonus(StatType stat)
        {
            return _bonuses.TryGetValue(stat, out int bonus) ? bonus : 0;
        }
    }
}
