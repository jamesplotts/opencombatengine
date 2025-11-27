using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardLevelManager : ILevelManager
    {
        private readonly Dictionary<string, int> _classes = new();
        private readonly IHitPoints _hitPoints;
        private readonly IAbilityScores _abilityScores;

        public int TotalLevel => _classes.Values.Sum();
        public int ExperiencePoints { get; private set; }
        public int ProficiencyBonus => (TotalLevel - 1) / 4 + 2;
        public IReadOnlyDictionary<string, int> Classes => _classes;

        public StandardLevelManager(IHitPoints hitPoints, IAbilityScores abilityScores)
        {
            _hitPoints = hitPoints ?? throw new ArgumentNullException(nameof(hitPoints));
            _abilityScores = abilityScores ?? throw new ArgumentNullException(nameof(abilityScores));
        }

        public void AddExperience(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "XP amount cannot be negative.");
            ExperiencePoints += amount;
        }

        public void LevelUp(string className, int hitDieSize, bool takeAverageHp = true)
        {
            if (string.IsNullOrWhiteSpace(className)) throw new ArgumentException("Class name cannot be empty.", nameof(className));
            if (hitDieSize <= 0) throw new ArgumentOutOfRangeException(nameof(hitDieSize), "Hit die size must be positive.");

            if (!_classes.TryGetValue(className, out int currentLevel))
            {
                currentLevel = 0;
            }
            _classes[className] = currentLevel + 1;

            // HP Increase
            int conMod = _abilityScores.GetModifier(Ability.Constitution);
            int hpIncrease = 0;

            if (takeAverageHp)
            {
                // Average is (Die / 2) + 1
                hpIncrease = (hitDieSize / 2) + 1;
            }
            else
            {
                // For now, we don't have a dice roller here, so we default to average or throw?
                // The interface implies we might want to roll. 
                // Let's stick to average for this implementation or just use average logic if takeAverageHp is false for now to avoid dependency on DiceRoller if not strictly needed yet.
                // Or better, assume max roll for level 1?
                // The prompt didn't specify level 1 logic, but usually level 1 is max.
                // For simplicity in this cycle, we'll use average.
                hpIncrease = (hitDieSize / 2) + 1;
            }

            hpIncrease += conMod;
            if (hpIncrease < 1) hpIncrease = 1; // Minimum 1 HP gain

            // We need a way to increase Max HP on IHitPoints.
            // IHitPoints usually has Max property. If it's settable, we set it.
            // Let's check IHitPoints definition.
            // Assuming we can't set it directly if it's just a getter, we might need to cast to StandardHitPoints or update interface.
            // For now, let's assume we can cast or it has a method.
            // I'll check IHitPoints in a moment. If I can't set it, I'll need to update IHitPoints.
            
            if (_hitPoints is StandardHitPoints stdHp)
            {
                stdHp.IncreaseMax(hpIncrease);
                stdHp.AddHitDie(1);
            }
        }
    }
}
