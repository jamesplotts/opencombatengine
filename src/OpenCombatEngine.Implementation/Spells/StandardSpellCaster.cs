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

        public bool HasSlot(int level)
        {
            if (level == 0) return true; // Cantrips
            return _currentSlots.TryGetValue(level, out int count) && count > 0;
        }

        public int GetSlots(int level)
        {
            return _currentSlots.TryGetValue(level, out int count) ? count : 0;
        }

        public int GetMaxSlots(int level)
        {
            return _maxSlots.TryGetValue(level, out int count) ? count : 0;
        }

        public Result<bool> ConsumeSlot(int level)
        {
            if (level == 0) return Result<bool>.Success(true); // Cantrips don't consume slots
            
            if (_currentSlots.TryGetValue(level, out int count) && count > 0)
            {
                _currentSlots[level] = count - 1;
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
