using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Classes;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardLevelManager : ILevelManager
    {
        private readonly Dictionary<IClassDefinition, int> _classes = new();
        private readonly ICreature _creature;

        public int TotalLevel => _classes.Values.Sum();
        public int ExperiencePoints { get; private set; }
        public int ProficiencyBonus => (TotalLevel - 1) / 4 + 2;
        public IReadOnlyDictionary<IClassDefinition, int> Classes => _classes;

        public StandardLevelManager(ICreature creature)
        {
            _creature = creature ?? throw new ArgumentNullException(nameof(creature));
        }

        public void AddExperience(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "XP amount cannot be negative.");
            ExperiencePoints += amount;
        }

        public void LevelUp(IClassDefinition classDefinition, bool takeAverageHp = true)
        {
            ArgumentNullException.ThrowIfNull(classDefinition);

            if (!_classes.TryGetValue(classDefinition, out int currentLevel))
            {
                currentLevel = 0;
            }
            int newLevel = currentLevel + 1;
            _classes[classDefinition] = newLevel;

            // HP Increase
            int conMod = _creature.AbilityScores.GetModifier(Ability.Constitution);
            int hpIncrease = 0;

            if (newLevel == 1)
            {
                // Level 1: Max Die + Con Mod
                hpIncrease = classDefinition.HitDie;
            }
            else
            {
                if (takeAverageHp)
                {
                    // Average is (Die / 2) + 1
                    hpIncrease = (classDefinition.HitDie / 2) + 1;
                }
                else
                {
                    // Default to average if no roller
                    hpIncrease = (classDefinition.HitDie / 2) + 1;
                }
            }

            hpIncrease += conMod;
            if (hpIncrease < 1) hpIncrease = 1; // Minimum 1 HP gain

            if (_creature.HitPoints is StandardHitPoints stdHp)
            {
                stdHp.IncreaseMax(hpIncrease);
                stdHp.AddHitDie(1);
            }

            // Apply Features
            if (classDefinition.FeaturesByLevel.TryGetValue(newLevel, out var features))
            {
                foreach (var feature in features)
                {
                    _creature.AddFeature(feature);
                }
            }
        }
    }
}
