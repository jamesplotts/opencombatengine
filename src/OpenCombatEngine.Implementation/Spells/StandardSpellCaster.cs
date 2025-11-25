using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Implementation.Spells
{
    public class StandardSpellCaster : ISpellCaster
    {
        private readonly List<ISpell> _knownSpells = new();
        private readonly List<ISpell> _preparedSpells = new();
        private readonly SpellSlotManager _slotManager = new();
        private readonly OpenCombatEngine.Core.Interfaces.Creatures.IAbilityScores _abilityScores;
        private readonly int _proficiencyBonus;
        private readonly OpenCombatEngine.Core.Enums.Ability _spellcastingAbility;

        private readonly bool _isPreparedCaster;

        public IReadOnlyList<ISpell> KnownSpells => _knownSpells.AsReadOnly();
        public IReadOnlyList<ISpell> PreparedSpells => _isPreparedCaster ? _preparedSpells.AsReadOnly() : _knownSpells.AsReadOnly();

        public int SpellSaveDC
        {
            get
            {
                int dc = 8 + _proficiencyBonus + _abilityScores.GetModifier(_spellcastingAbility);
                if (_effects != null)
                {
                    dc = _effects.ApplyStatBonuses(OpenCombatEngine.Core.Enums.StatType.SpellSaveDC, dc);
                }
                return dc;
            }
        }

        public int SpellAttackBonus
        {
            get
            {
                int bonus = _proficiencyBonus + _abilityScores.GetModifier(_spellcastingAbility);
                if (_effects != null)
                {
                    bonus = _effects.ApplyStatBonuses(OpenCombatEngine.Core.Enums.StatType.AttackRoll, bonus);
                }
                return bonus;
            }
        }

        private OpenCombatEngine.Core.Interfaces.Effects.IEffectManager? _effects;

        public void SetEffectManager(OpenCombatEngine.Core.Interfaces.Effects.IEffectManager effects)
        {
            _effects = effects;
        }

        public ISpell? ConcentratingOn { get; private set; }
        public Ability CastingAbility { get; }

        public void BreakConcentration()
        {
            if (ConcentratingOn != null)
            {
                // TODO: Notify that concentration ended?
                // For now just clear it.
                ConcentratingOn = null;
            }
        }

        public void SetConcentration(ISpell spell)
        {
            ArgumentNullException.ThrowIfNull(spell);
            
            if (ConcentratingOn != null)
            {
                BreakConcentration();
            }
            
            ConcentratingOn = spell;
        }

        public StandardSpellCaster(
            OpenCombatEngine.Core.Interfaces.Creatures.IAbilityScores abilityScores,
            int proficiencyBonus,
            OpenCombatEngine.Core.Enums.Ability spellcastingAbility,
            bool isPreparedCaster = false)
        {
            ArgumentNullException.ThrowIfNull(abilityScores);
            _abilityScores = abilityScores;
            _proficiencyBonus = proficiencyBonus;
            _spellcastingAbility = spellcastingAbility;
            _isPreparedCaster = isPreparedCaster;
            CastingAbility = spellcastingAbility;
        }

        public bool HasSlot(int level) => _slotManager.HasSlot(level);
        public int GetSlots(int level) => _slotManager.GetCurrentSlots(level);
        public int GetMaxSlots(int level) => _slotManager.GetMaxSlots(level);
        public Result<bool> ConsumeSlot(int level) => _slotManager.ConsumeSlot(level);
        public void RestoreAllSlots() => _slotManager.RestoreAllSlots();
        public void SetSlots(int level, int max) => _slotManager.SetSlots(level, max);

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
            ArgumentNullException.ThrowIfNull(spell);
            if (!_knownSpells.Any(s => s.Name == spell.Name))
            {
                return Result<bool>.Failure($"Cannot prepare unknown spell: {spell.Name}");
            }
            if (!_preparedSpells.Any(s => s.Name == spell.Name))
            {
                _preparedSpells.Add(spell);
            }
            return Result<bool>.Success(true);
        }

        public void UnprepareSpell(ISpell spell)
        {
            ArgumentNullException.ThrowIfNull(spell);
            var existing = _preparedSpells.FirstOrDefault(s => s.Name == spell.Name);
            if (existing != null)
            {
                _preparedSpells.Remove(existing);
            }
        }
    }
}
