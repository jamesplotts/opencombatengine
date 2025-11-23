using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Spells
{
    public class SpellSlotManager
    {
        private readonly Dictionary<int, int> _maxSlots = new();
        private readonly Dictionary<int, int> _currentSlots = new();

        public void SetSlots(int level, int max)
        {
            if (level < 1) return; // Cantrips don't use slots
            _maxSlots[level] = max;
            // If current exceeds new max, clamp it? Or reset?
            // Usually this happens on level up.
            // Let's clamp.
            if (_currentSlots.TryGetValue(level, out int current))
            {
                if (current > max) _currentSlots[level] = max;
            }
            else
            {
                _currentSlots[level] = max; // Initialize full? Or empty?
                // Usually on level up you get full slots?
                // Let's assume full for now.
            }
        }

        public int GetMaxSlots(int level)
        {
            return _maxSlots.TryGetValue(level, out int max) ? max : 0;
        }

        public int GetCurrentSlots(int level)
        {
            return _currentSlots.TryGetValue(level, out int current) ? current : 0;
        }

        public bool HasSlot(int level)
        {
            return GetCurrentSlots(level) > 0;
        }

        public Result<bool> ConsumeSlot(int level)
        {
            if (level < 1) return Result<bool>.Success(true); // Cantrips

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
        
        // Future: Restore specific amount (Arcane Recovery)
    }
}
