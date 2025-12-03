using System;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Models;

namespace OpenCombatEngine.Implementation.Comparers
{
    public class StandardInitiativeComparer : IInitiativeComparer
    {
        public int Compare(InitiativeRoll? x, InitiativeRoll? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1; // Null is "smaller" (comes last in descending?) 
            // Wait, IComparer logic: < 0 if x < y, 0 if x == y, > 0 if x > y.
            // We want Descending order usually (High Roll first).
            // But List.Sort uses Ascending by default.
            // So if we want High Roll first, we should return -1 if x > y (so x comes before y).
            // OR we can implement standard comparison (x < y returns -1) and use OrderByDescending or List.Sort((a,b) => -Compare(a,b)).
            // Let's implement standard "Compare": x vs y.
            // If x.Total > y.Total, x is "greater".
            
            if (y == null) return 1;

            // 1. Total Roll
            int totalComparison = x.Total.CompareTo(y.Total);
            if (totalComparison != 0) return totalComparison;

            // 2. Dexterity Score
            int dexComparison = x.DexterityScore.CompareTo(y.DexterityScore);
            if (dexComparison != 0) return dexComparison;

            // 3. Tie-Breaker (Deterministic based on ID)
            // We compare IDs to ensure stability.
            return x.Creature.Id.CompareTo(y.Creature.Id);
        }
    }
}
