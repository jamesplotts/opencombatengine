// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Dice
{
    /// <summary>
    /// Provides dice rolling functionality with support for standard notation and SRD 5.1 mechanics
    /// </summary>
    /// <remarks>
    /// This interface supports standard dice notation (e.g., "3d6+2", "1d20-1") and
    /// core SRD mechanics like advantage and disadvantage. Advanced features like
    /// exploding dice or keep highest/lowest should be implemented in extension interfaces.
    /// </remarks>
    public interface IDiceRoller
    {
        /// <summary>
        /// Rolls dice according to the specified notation
        /// </summary>
        /// <param name="notation">Dice notation string (e.g., "3d6+2", "1d20", "2d4-1")</param>
        /// <returns>Result containing the roll total and details, or failure with error message</returns>
        /// <remarks>
        /// Supported notation patterns:
        /// - Standard: "XdY" where X is count, Y is sides (e.g., "3d6")
        /// - With modifier: "XdY+Z" or "XdY-Z" (e.g., "2d8+5")
        /// - Single die: "dY" is interpreted as "1dY"
        /// - Constants: Plain numbers like "5" return that value
        /// </remarks>
        /// <example>
        /// var result = roller.Roll("3d6+2");
        /// if (result.IsSuccess)
        ///     Console.WriteLine($"Rolled {result.Value.Total}");
        /// </example>
        Result<DiceRollResult> Roll(string notation);

        /// <summary>
        /// Rolls dice with advantage (roll twice, take higher)
        /// </summary>
        /// <param name="notation">Dice notation string, typically "1d20" for ability checks</param>
        /// <returns>Result containing the higher of two rolls, or failure with error message</returns>
        /// <remarks>
        /// Advantage is a core SRD 5.1 mechanic where you roll twice and use the higher result.
        /// Both rolls are preserved in the result for transparency.
        /// </remarks>
        Result<DiceRollResult> RollWithAdvantage(string notation);

        /// <summary>
        /// Rolls dice with disadvantage (roll twice, take lower)
        /// </summary>
        /// <param name="notation">Dice notation string, typically "1d20" for ability checks</param>
        /// <returns>Result containing the lower of two rolls, or failure with error message</returns>
        /// <remarks>
        /// Disadvantage is a core SRD 5.1 mechanic where you roll twice and use the lower result.
        /// Both rolls are preserved in the result for transparency.
        /// </remarks>
        Result<DiceRollResult> RollWithDisadvantage(string notation);

        /// <summary>
        /// Validates if a dice notation string is properly formatted
        /// </summary>
        /// <param name="notation">Dice notation string to validate</param>
        /// <returns>True if notation is valid, false otherwise</returns>
        /// <remarks>
        /// This method checks syntax only, not semantic validity (e.g., "1d3" is syntactically
        /// valid even though 3-sided dice don't physically exist).
        /// </remarks>
        bool IsValidNotation(string notation);

        /// <summary>
        /// Gets or sets the random number generator seed for reproducible results
        /// </summary>
        /// <remarks>
        /// Setting a specific seed enables reproducible dice rolls for testing or replay functionality.
        /// Set to null for true randomness.
        /// </remarks>
        int? Seed { get; set; }
    }
}
