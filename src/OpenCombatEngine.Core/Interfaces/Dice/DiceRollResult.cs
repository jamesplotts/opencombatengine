// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace OpenCombatEngine.Core.Interfaces.Dice
{
    /// <summary>
    /// Contains the complete results of a dice roll including total and individual die results
    /// </summary>
    /// <param name="Total">The final calculated total including all dice and modifiers</param>
    /// <param name="Notation">The original notation string that was rolled</param>
    /// <param name="IndividualRolls">List of individual die roll results</param>
    /// <param name="Modifier">The numeric modifier applied after dice rolls</param>
    /// <param name="RollType">The type of roll performed (normal, advantage, disadvantage)</param>
    /// <param name="AlternateRoll">For advantage/disadvantage, contains the roll that wasn't used</param>
    public record DiceRollResult(
        int Total,
        string Notation,
        IReadOnlyList<int> IndividualRolls,
        int Modifier,
        RollType RollType,
        DiceRollResult? AlternateRoll = null)
    {
        /// <summary>
        /// Gets the sum of individual dice rolls before applying the modifier
        /// </summary>
        public int DiceTotal => IndividualRolls?.Sum() ?? 0;

        /// <summary>
        /// Gets the number of dice that were rolled
        /// </summary>
        public int DiceCount => IndividualRolls?.Count ?? 0;

        /// <summary>
        /// Gets whether this was a critical success (natural 20 on a d20)
        /// </summary>
        /// <remarks>
        /// Only applicable for single d20 rolls per SRD rules
        /// </remarks>
        public bool IsCriticalSuccess => 
            DiceCount == 1 && 
            (IndividualRolls != null && IndividualRolls.Count > 0 && IndividualRolls[0] == 20) &&
            Notation.Contains("d20", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets whether this was a critical failure (natural 1 on a d20)
        /// </summary>
        /// <remarks>
        /// Only applicable for single d20 rolls per SRD rules
        /// </remarks>
        public bool IsCriticalFailure => 
            DiceCount == 1 && 
            (IndividualRolls != null && IndividualRolls.Count > 0 && IndividualRolls[0] == 1) &&
            Notation.Contains("d20", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a simple result for a constant value
        /// </summary>
        /// <param name="value">The constant value</param>
        /// <returns>A DiceRollResult representing the constant</returns>
        public static DiceRollResult Constant(int value) =>
            new(value, value.ToString(CultureInfo.InvariantCulture), Array.Empty<int>(), value, RollType.Normal);

        /// <summary>
        /// Provides a formatted string representation of the roll
        /// </summary>
        /// <returns>Human-readable description of the roll result</returns>
        public override string ToString()
        {
            var result = $"{Notation} = ";
            
            if (IndividualRolls?.Count > 0)
            {
                result += $"[{string.Join(", ", IndividualRolls)}]";
                if (Modifier != 0)
                {
                    result += Modifier > 0 ? $" + {Modifier}" : $" - {Math.Abs(Modifier)}";
                }
                result += $" = {Total}";
            }
            else
            {
                result += Total.ToString(CultureInfo.InvariantCulture);
            }

            if (RollType == RollType.Advantage && AlternateRoll != null)
            {
                result += $" (advantage, other roll: {AlternateRoll.Total})";
            }
            else if (RollType == RollType.Disadvantage && AlternateRoll != null)
            {
                result += $" (disadvantage, other roll: {AlternateRoll.Total})";
            }

            if (IsCriticalSuccess) result += " [CRITICAL SUCCESS!]";
            if (IsCriticalFailure) result += " [CRITICAL FAILURE!]";

            return result;
        }
    }
}
