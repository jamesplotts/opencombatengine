using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Spells
{
    public class StandardSpellCaster : ISpellCaster
    {
        private readonly List<ISpell> _knownSpells = new();
        private readonly Dictionary<int, int> _maxSlots = new();
        private readonly Dictionary<int, int> _currentSlots = new();

        public IReadOnlyList<ISpell> KnownSpells => _knownSpells.AsReadOnly();

        public bool HasSlot(int level)
        {
            if (level == 0) return true; // Cantrips don't use slots
            return _currentSlots.TryGetValue(level, out int slots) && slots > 0;
        }

        public int GetSlots(int level)
        {
            return _currentSlots.TryGetValue(level, out int slots) ? slots : 0;
        }

        public int GetMaxSlots(int level)
        {
            return _maxSlots.TryGetValue(level, out int slots) ? slots : 0;
        }

        public Result<bool> ConsumeSlot(int level)
        {
            if (level == 0) return Result<bool>.Success(true); // Cantrips

            if (!HasSlot(level))
            {
                return Result<bool>.Failure($"No spell slots available for level {level}.");
            }

            _currentSlots[level]--;
            return Result<bool>.Success(true);
        }

        public void RestoreAllSlots()
        {
            foreach (var level in _maxSlots.Keys)
            {
                _currentSlots[level] = _maxSlots[level];
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

        public void SetSlots(int level, int max)
        {
            if (level < 1 || level > 9) throw new ArgumentOutOfRangeException(nameof(level), "Slot level must be between 1 and 9.");
            if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), "Max slots cannot be negative.");

            _maxSlots[level] = max;
            // If we increase max, should we increase current? Usually yes on level up.
            // If we decrease, we clamp.
            // For simplicity, let's reset current to max when setting max, assuming this happens on initialization or level up.
            // Or should we preserve current?
            // If we are just setting capacity, maybe preserve.
            // But if we are initializing, we want full.
            // Let's set current to max for now.
            _currentSlots[level] = max;
        }
    }
}
