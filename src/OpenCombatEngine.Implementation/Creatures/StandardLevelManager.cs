using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Core.Interfaces;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardLevelManager : ILevelManager, IStateful<LevelManagerState>
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

        public StandardLevelManager(ICreature creature, LevelManagerState state) : this(creature)
        {
            ArgumentNullException.ThrowIfNull(state);
            ExperiencePoints = state.ExperiencePoints;
            foreach (var classState in state.Classes)
            {
                // Reconstruct basic class definition. 
                // Features are lost in basic restoration unless we look them up or have a registry.
                // For now, we restore the definition so levels are correct.
                var def = new OpenCombatEngine.Implementation.Classes.ClassDefinition(classState.ClassName, classState.HitDie);
                _classes[def] = classState.Level;
            }
        }

        public LevelManagerState GetState()
        {
            var classStates = new System.Collections.ObjectModel.Collection<ClassLevelState>();
            foreach (var kvp in _classes)
            {
                classStates.Add(new ClassLevelState(kvp.Key.Name, kvp.Value, kvp.Key.HitDie));
            }
            return new LevelManagerState(ExperiencePoints, classStates);
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

            // Update Spell Slots
            UpdateSpellSlots();
        }

        private void UpdateSpellSlots()
        {
            if (_creature.Spellcasting == null) return;

            var castingClasses = new List<(OpenCombatEngine.Core.Enums.SpellcastingType Type, int Level)>();
            foreach (var kvp in _classes)
            {
                if (kvp.Key.SpellcastingType != OpenCombatEngine.Core.Enums.SpellcastingType.None)
                {
                    castingClasses.Add((kvp.Key.SpellcastingType, kvp.Value));
                }
            }

            if (castingClasses.Count > 0)
            {
                var slots = OpenCombatEngine.Implementation.Spells.SpellSlotCalculator.CalculateSlots(castingClasses);
                
                // Set slots on caster
                for (int i = 1; i <= 9; i++)
                {
                    if (slots.TryGetValue(i, out int count))
                    {
                        _creature.Spellcasting.SetSlots(i, count);
                    }
                    else
                    {
                        _creature.Spellcasting.SetSlots(i, 0);
                    }
                }
            }

            // Pact Slots (Warlock)
            // Separate logic. Find total Pact Level.
            // Assuming Warlock is the only class with Pact Magic for now.
            // If multiple Pact classes exist, levels stack.
            int pactLevel = 0;
            foreach (var kvp in _classes)
            {
                if (kvp.Key.SpellcastingType == OpenCombatEngine.Core.Enums.SpellcastingType.Pact)
                {
                    pactLevel += kvp.Value;
                }
            }

            if (pactLevel > 0 && _creature.Spellcasting is OpenCombatEngine.Implementation.Spells.StandardSpellCaster ssc)
            {
                // Calculate Pact Slots
                // 1-10: 2 slots? Wait.
                // 1: 1
                // 2-10: 2
                // 11-16: 3
                // 17+: 4
                int quantity = 0;
                if (pactLevel >= 17) quantity = 4;
                else if (pactLevel >= 11) quantity = 3;
                else if (pactLevel >= 2) quantity = 2;
                else quantity = 1;

                // Calculate Pact Slot Level
                // 1-2: 1st
                // 3-4: 2nd
                // 5-6: 3rd
                // 7-8: 4th
                // 9+: 5th
                int level = 1;
                if (pactLevel >= 9) level = 5;
                else if (pactLevel >= 7) level = 4;
                else if (pactLevel >= 5) level = 3;
                else if (pactLevel >= 3) level = 2;

                ssc.SetPactSlots(quantity, level);
            }
        }
    }
}
