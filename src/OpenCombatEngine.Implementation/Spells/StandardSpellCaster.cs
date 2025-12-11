using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Spells
{
    public class StandardSpellCaster : ISpellCaster
    {
        private readonly List<ISpell> _knownSpells = new();
        private readonly List<ISpell> _preparedSpells = new();
        private readonly Dictionary<int, int> _maxSlots = new();
        private readonly Dictionary<int, int> _currentSlots = new();
        
        private IEffectManager? _effectManager;
        private readonly Ability _castingAbility;
        // private readonly int _proficiencyBonus; // Removed unused field
        // Constructing with fixed PB is risky if it changes.
        // But Interface doesn't have "ICreature Owner".
        // It has SetEffectManager.
        // Maybe we pass a func for PB and Mod?
        // Or we pass ICreature in constructor but don't expose it.
        // For now, I'll take a Func<int> for PB and Func<int> for Mod.
        // Or simpler: StandardSpellCaster is held by Creature. Creature updates it?
        // Let's take casting ability and a way to resolve modifiers.
        
        private readonly Func<Ability, int> _getModifier;
        private readonly Func<int> _getProficiency;
        private readonly bool _isPreparedCaster;

        public StandardSpellCaster(Ability castingAbility, Func<Ability, int> getModifier, Func<int> getProficiency, bool isPreparedCaster = true)
        {
            _castingAbility = castingAbility;
            _getModifier = getModifier ?? throw new ArgumentNullException(nameof(getModifier));
            _getProficiency = getProficiency ?? throw new ArgumentNullException(nameof(getProficiency));
            _isPreparedCaster = isPreparedCaster;
        }

        public IReadOnlyList<ISpell> KnownSpells => _knownSpells.AsReadOnly();
        public IReadOnlyList<ISpell> PreparedSpells => _isPreparedCaster ? _preparedSpells.AsReadOnly() : _knownSpells.AsReadOnly();
        
        public Ability CastingAbility => _castingAbility;

        public int SpellSaveDC
        {
            get
            {
                int baseDC = 8 + _getProficiency() + _getModifier(_castingAbility);
                if (_effectManager != null)
                {
                    baseDC = _effectManager.ApplyStatBonuses(StatType.SpellSaveDC, baseDC);
                }
                return baseDC;
            }
        }

        public int SpellAttackBonus
        {
            get
            {
                int bonus = _getProficiency() + _getModifier(_castingAbility);
                if (_effectManager != null)
                {
                    bonus = _effectManager.ApplyStatBonuses(StatType.SpellAttackBonus, bonus);
                }
                return bonus;
            }
        }

        public ISpell? ConcentratingOn { get; private set; }

        // Pact Magic Tracking
        public int PactSlotsMax { get; private set; }
        public int PactSlotsCurrent { get; private set; }
        public int PactSlotLevel { get; private set; }

        public bool HasSlot(int level)
        {
            if (level == 0) return true; // Cantrips
            if (_currentSlots.TryGetValue(level, out int count) && count > 0) return true;
            
            // Check Pact Slots
            // Pact Slots are of a specific level. You can use them for that level.
            // You can also use them for LOWER level spells (upcast).
            // But HasSlot(level) usually asks "Do I have a slot OF this level?"
            // DND rules: "You can use a spell slot of a higher level..."
            // But this method checks direct availability usually?
            // Let's say yes, if PactSlotLevel >= level and PactSlotsCurrent > 0.
            if (PactSlotsCurrent > 0 && PactSlotLevel >= level) return true;
            
            return false;
        }

        public int GetSlots(int level)
        {
            int count = _currentSlots.TryGetValue(level, out int c) ? c : 0;
            // Should we add pact slots here? 
            // If PactSlotLevel == level, add them?
            if (PactSlotLevel == level) count += PactSlotsCurrent;
            return count;
        }

        public int GetMaxSlots(int level)
        {
            int count = _maxSlots.TryGetValue(level, out int c) ? c : 0;
            if (PactSlotLevel == level) count += PactSlotsMax;
            return count;
        }

        public Result<bool> ConsumeSlot(int level)
        {
            if (level == 0) return Result<bool>.Success(true); 
            
            // Priority: Use standard slot if available at exact level?
            // Or use Pact Slot if equal level?
            // Let's prioritize Standard Slots unless they are empty.
            
            if (_currentSlots.TryGetValue(level, out int count) && count > 0)
            {
                _currentSlots[level] = count - 1;
                return Result<bool>.Success(true);
            }
            
            // If no standard slots, try Pact Slots
            // Valid if PactSlotLevel >= level
            if (PactSlotsCurrent > 0 && PactSlotLevel >= level)
            {
                PactSlotsCurrent--;
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure($"No level {level} slots available.");
        }

        public void RestoreAllSlots()
        {
            foreach (var kvp in _maxSlots)
            {
                _currentSlots[kvp.Key] = kvp.Value;
            }
            // Restore Pact Slots too (Long Rest restores all)
            PactSlotsCurrent = PactSlotsMax;
        }

        public void RestoreShortRestSlots()
        {
            PactSlotsCurrent = PactSlotsMax;
            // Standard slots not restored (unless Wizard Arcane Recovery etc, but that's a feature, not default)
        }

        public void SetPactSlots(int quantity, int level)
        {
            int oldMax = PactSlotsMax;
            PactSlotsMax = quantity;
            PactSlotLevel = level;
            
            // If gaining slots, add the difference to current.
            if (PactSlotsMax > oldMax)
            {
                PactSlotsCurrent += (PactSlotsMax - oldMax);
            }
            
            // Cap current
            if (PactSlotsCurrent > PactSlotsMax) PactSlotsCurrent = PactSlotsMax;
        }

        public void LearnSpell(ISpell spell)
        {
            ArgumentNullException.ThrowIfNull(spell);
            if (!_knownSpells.Any(s => s.Name == spell.Name))
            {
                _knownSpells.Add(spell);
            }
        }

        public Result<bool> PrepareSpell(ISpell spell)
        {
            if (!_isPreparedCaster) return Result<bool>.Failure("Caster does not prepare spells.");
            ArgumentNullException.ThrowIfNull(spell);
            if (!_knownSpells.Any(s => s.Name == spell.Name))
            {
                return Result<bool>.Failure("Cannot prepare unknown spell.");
            }
            
            if (!_preparedSpells.Any(s => s.Name == spell.Name))
            {
                _preparedSpells.Add(spell);
            }
            return Result<bool>.Success(true);
        }

        public void UnprepareSpell(ISpell spell)
        {
            var match = _preparedSpells.FirstOrDefault(s => s.Name == spell.Name);
            if (match != null)
            {
                _preparedSpells.Remove(match);
            }
        }

        public void UnlearnSpell(ISpell spell)
        {
            var match = _knownSpells.FirstOrDefault(s => s.Name == spell.Name);
            if (match != null)
            {
                _knownSpells.Remove(match);
                UnprepareSpell(match);
            }
        }

        public void SetSlots(int level, int max)
        {
            if (level < 1 || level > 9) return;
            _maxSlots[level] = max;
            // If current is less than new max? Or reset?
            // Usually setting slots implies leveling up.
            // We should ensure current doesn't exceed max?
            // Or just init current to max if it was missing.
            if (!_currentSlots.ContainsKey(level))
            {
                _currentSlots[level] = max;
            }
            // If we reduce max, cap current.
            if (_currentSlots.TryGetValue(level, out int current) && current > max)
            {
                _currentSlots[level] = max;
            }
        }

        public void SetEffectManager(IEffectManager effects)
        {
            _effectManager = effects;
        }

        public void BreakConcentration()
        {
            ConcentratingOn = null;
        }

        public void SetConcentration(ISpell spell)
        {
            ConcentratingOn = spell;
        }
    }
}
