// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;

namespace OpenCombatEngine.Core.Interfaces.Loot
{
    /// <summary>
    /// Combines the individual SRD challenge ratings of a group of
    /// creatures into a single effective challenge rating appropriate for
    /// generating loot for the whole group at once — e.g. an evil high
    /// priest, three acolytes, and ten guards should net more total
    /// treasure than the priest alone, not be capped at whatever his own
    /// CR alone would afford. Deliberately lives on the system-engine side
    /// of the gRPC boundary (docs/design.md §6.1): this is game-specific
    /// math tied to this SRD's own encounter-building rules, and a
    /// different system engine (e.g. a Vampire: The Masquerade
    /// implementation) would have no equivalent concept at all.
    /// </summary>
    public interface IEncounterChallengeCalculator
    {
        /// <summary>
        /// Computes the single effective challenge rating for a group of
        /// creatures' challenge ratings, using the standard CR-to-XP
        /// conversion and monster-count XP multiplier. A single-element
        /// input reduces to that creature's own challenge rating (a
        /// one-monster group's multiplier is x1). An empty input returns 0.
        /// </summary>
        double CalculateEffectiveChallengeRating(IEnumerable<double> challengeRatings);
    }
}
