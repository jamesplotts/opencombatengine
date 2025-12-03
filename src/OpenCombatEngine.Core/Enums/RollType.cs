// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

namespace OpenCombatEngine.Core.Interfaces.Dice
{
    /// <summary>
    /// Specifies the type of dice roll performed
    /// </summary>
    public enum RollType
    {
        /// <summary>Unknown or unspecified roll type</summary>
        Unspecified = 0,

        /// <summary>Standard dice roll with no special modifiers</summary>
        Normal,

        /// <summary>Roll with advantage (roll twice, use higher)</summary>
        Advantage,

        /// <summary>Roll with disadvantage (roll twice, use lower)</summary>
        Disadvantage,

        /// <summary>Sentinel value for validation</summary>
        LastValue
    }

    /// <summary>
    /// Extension methods for RollType enum validation
    /// </summary>
    public static class RollTypeExtensions
    {
        /// <summary>
        /// Validates if a RollType value is within valid range
        /// </summary>
        /// <param name="rollType">The roll type to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValid(this RollType rollType) =>
            rollType > RollType.Unspecified && rollType < RollType.LastValue;
    }
}
