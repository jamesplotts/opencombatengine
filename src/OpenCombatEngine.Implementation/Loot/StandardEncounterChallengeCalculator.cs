// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Loot;

namespace OpenCombatEngine.Implementation.Loot
{
    /// <summary>
    /// Standard SRD-style implementation of
    /// <see cref="IEncounterChallengeCalculator"/>: converts each creature's
    /// CR to its XP value, sums them, scales by the standard monster-count
    /// XP multiplier, then maps the resulting XP budget back down to the
    /// single highest CR it would afford on its own — that one effective
    /// CR is what <see cref="ILootGenerator.GenerateLoot"/> is actually
    /// tuned for (its own CR-tiered dice formulas), so this does not invent
    /// a second, incompatible scaling axis.
    /// </summary>
    public class StandardEncounterChallengeCalculator : IEncounterChallengeCalculator
    {
        // CR -> XP, the standard SRD/DMG conversion table. Ordered
        // ascending by CR; both directions of the lookup below rely on
        // that ordering.
        private static readonly (double Cr, int Xp)[] ChallengeRatingXp =
        {
            (0d, 10), (0.125d, 25), (0.25d, 50), (0.5d, 100),
            (1d, 200), (2d, 450), (3d, 700), (4d, 1100),
            (5d, 1800), (6d, 2300), (7d, 2900), (8d, 3900),
            (9d, 5000), (10d, 5900), (11d, 7200), (12d, 8400),
            (13d, 10000), (14d, 11500), (15d, 13000), (16d, 15000),
            (17d, 18000), (18d, 20000), (19d, 22000), (20d, 25000),
            (21d, 33000), (22d, 41000), (23d, 50000), (24d, 62000),
            (25d, 75000), (26d, 90000), (27d, 105000), (28d, 120000),
            (29d, 135000), (30d, 155000),
        };

        // Standard DMG-style "adjusting for multiple monsters" XP
        // multiplier, keyed by the total number of creatures in the group.
        private static double MultiplierForCount(int count) => count switch
        {
            <= 1 => 1.0,
            2 => 1.5,
            <= 6 => 2.0,
            <= 10 => 2.5,
            <= 14 => 3.0,
            _ => 4.0,
        };

        public double CalculateEffectiveChallengeRating(IEnumerable<double> challengeRatings)
        {
            var list = challengeRatings?.ToList() ?? new List<double>();
            if (list.Count == 0) return 0d;

            var totalXp = list.Sum(XpForChallengeRating);
            var adjustedXp = totalXp * MultiplierForCount(list.Count);
            return ChallengeRatingForXpBudget(adjustedXp);
        }

        private static int XpForChallengeRating(double cr)
        {
            // Exact match expected (CR values come from the fixed SRD set);
            // an out-of-table value (a DM typo, or a CR above 30) falls
            // back to the nearest table entry rather than throwing, since
            // this is loot flavor, not a mechanical legality gate.
            foreach (var entry in ChallengeRatingXp)
            {
                if (entry.Cr == cr) return entry.Xp;
            }

            return ChallengeRatingXp
                .OrderBy(e => System.Math.Abs(e.Cr - cr))
                .First().Xp;
        }

        private static double ChallengeRatingForXpBudget(double xpBudget)
        {
            var affordable = ChallengeRatingXp.Where(e => e.Xp <= xpBudget).ToList();
            return affordable.Count > 0 ? affordable.Max(e => e.Cr) : 0d;
        }
    }
}
