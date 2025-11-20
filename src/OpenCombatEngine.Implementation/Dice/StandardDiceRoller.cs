// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpenCombatEngine.Implementation.Dice
{
    /// <summary>
    /// Standard implementation of IDiceRoller supporting basic dice notation and SRD 5.1 mechanics
    /// </summary>
    public class StandardDiceRoller : IDiceRoller
    {
        #region Private Fields

        private readonly ILogger<StandardDiceRoller> _logger;
        private Random _random;
        private int? _seed;

        // Regex pattern for dice notation: [count]d<sides>[+/-modifier]
        private static readonly Regex DiceNotationRegex = new Regex(
            @"^\s*(?:(?<count>\d+)?d(?<sides>\d+)(?<modifier>[+-]\d+)?|(?<constant>-?\d+))\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const int MAX_DICE_COUNT = 100;
        private const int MAX_DIE_SIDES = 1000;
        private const int MIN_DIE_SIDES = 2;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the StandardDiceRoller class
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic output</param>
        public StandardDiceRoller(ILogger<StandardDiceRoller> logger = null)
        {
            _logger = logger ?? NullLogger<StandardDiceRoller>.Instance;
            _random = new Random();
            _logger.LogDebug("StandardDiceRoller initialized with random seed");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the random number generator seed for reproducible results
        /// </summary>
        public int? Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                _random = value.HasValue 
                    ? new Random(value.Value) 
                    : new Random();
                
                _logger.LogDebug(value.HasValue 
                    ? $"Seed set to {value.Value}" 
                    : "Seed cleared, using random seed");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Rolls dice according to the specified notation
        /// </summary>
        /// <param name="notation">Dice notation string (e.g., "3d6+2", "1d20", "2d4-1")</param>
        /// <returns>Result containing the roll total and details, or failure with error message</returns>
        public Result<DiceRollResult> Roll(string notation)
        {
            // Validate input
            if (!IsValidNotation(notation))
            {
                var error = $"Invalid dice notation: '{notation}'";
                _logger.LogWarning(error);
                return Result<DiceRollResult>.Failure(error);
            }

            try
            {
                // Parse the notation
                var parsedNotation = ParseNotation(notation);
                if (!parsedNotation.IsSuccess)
                {
                    return Result<DiceRollResult>.Failure(parsedNotation.Error);
                }

                var (diceCount, dieSides, modifier, isConstant) = parsedNotation.Value;

                // Handle constant values
                if (isConstant)
                {
                    _logger.LogDebug($"Rolling constant value: {modifier}");
                    return Result<DiceRollResult>.Success(DiceRollResult.Constant(modifier));
                }

                // Roll the dice
                var individualRolls = RollDice(diceCount, dieSides);
                var diceTotal = individualRolls.Sum();
                var total = diceTotal + modifier;

                var result = new DiceRollResult(
                    Total: total,
                    Notation: notation.Trim(),
                    IndividualRolls: individualRolls,
                    Modifier: modifier,
                    RollType: RollType.Normal
                );

                _logger.LogDebug($"Rolled {notation}: {result}");
                return Result<DiceRollResult>.Success(result);
            }
            catch (Exception ex)
            {
                var error = $"Error rolling dice: {ex.Message}";
                _logger.LogError(ex, error);
                return Result<DiceRollResult>.Failure(error);
            }
        }

        /// <summary>
        /// Rolls dice with advantage (roll twice, take higher)
        /// </summary>
        /// <param name="notation">Dice notation string, typically "1d20" for ability checks</param>
        /// <returns>Result containing the higher of two rolls, or failure with error message</returns>
        public Result<DiceRollResult> RollWithAdvantage(string notation)
        {
            _logger.LogDebug($"Rolling with advantage: {notation}");

            // Roll twice
            var roll1 = Roll(notation);
            if (!roll1.IsSuccess)
                return roll1;

            var roll2 = Roll(notation);
            if (!roll2.IsSuccess)
                return roll2;

            // Select the higher roll
            var selectedRoll = roll1.Value.Total >= roll2.Value.Total ? roll1.Value : roll2.Value;
            var alternateRoll = roll1.Value.Total >= roll2.Value.Total ? roll2.Value : roll1.Value;

            var result = new DiceRollResult(
                Total: selectedRoll.Total,
                Notation: selectedRoll.Notation,
                IndividualRolls: selectedRoll.IndividualRolls,
                Modifier: selectedRoll.Modifier,
                RollType: RollType.Advantage,
                AlternateRoll: alternateRoll
            );

            _logger.LogDebug($"Advantage roll result: {result}");
            return Result<DiceRollResult>.Success(result);
        }

        /// <summary>
        /// Rolls dice with disadvantage (roll twice, take lower)
        /// </summary>
        /// <param name="notation">Dice notation string, typically "1d20" for ability checks</param>
        /// <returns>Result containing the lower of two rolls, or failure with error message</returns>
        public Result<DiceRollResult> RollWithDisadvantage(string notation)
        {
            _logger.LogDebug($"Rolling with disadvantage: {notation}");

            // Roll twice
            var roll1 = Roll(notation);
            if (!roll1.IsSuccess)
                return roll1;

            var roll2 = Roll(notation);
            if (!roll2.IsSuccess)
                return roll2;

            // Select the lower roll
            var selectedRoll = roll1.Value.Total <= roll2.Value.Total ? roll1.Value : roll2.Value;
            var alternateRoll = roll1.Value.Total <= roll2.Value.Total ? roll2.Value : roll1.Value;

            var result = new DiceRollResult(
                Total: selectedRoll.Total,
                Notation: selectedRoll.Notation,
                IndividualRolls: selectedRoll.IndividualRolls,
                Modifier: selectedRoll.Modifier,
                RollType: RollType.Disadvantage,
                AlternateRoll: alternateRoll
            );

            _logger.LogDebug($"Disadvantage roll result: {result}");
            return Result<DiceRollResult>.Success(result);
        }

        /// <summary>
        /// Validates if a dice notation string is properly formatted
        /// </summary>
        /// <param name="notation">Dice notation string to validate</param>
        /// <returns>True if notation is valid, false otherwise</returns>
        public bool IsValidNotation(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation))
                return false;

            var match = DiceNotationRegex.Match(notation);
            if (!match.Success)
                return false;

            // Check if it's a constant
            if (match.Groups["constant"].Success)
                return true;

            // Parse dice values
            var countStr = match.Groups["count"].Value;
            var sidesStr = match.Groups["sides"].Value;

            // Default count to 1 if not specified
            int diceCount = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);
            int dieSides = int.Parse(sidesStr);

            // Validate ranges
            if (diceCount < 1 || diceCount > MAX_DICE_COUNT)
                return false;
            
            if (dieSides < MIN_DIE_SIDES || dieSides > MAX_DIE_SIDES)
                return false;

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses dice notation into components
        /// </summary>
        /// <param name="notation">The notation to parse</param>
        /// <returns>Tuple of (diceCount, dieSides, modifier, isConstant)</returns>
        private Result<(int diceCount, int dieSides, int modifier, bool isConstant)> ParseNotation(string notation)
        {
            var match = DiceNotationRegex.Match(notation);
            
            if (!match.Success)
                return Result<(int, int, int, bool)>.Failure($"Failed to parse notation: {notation}");

            // Check for constant value
            if (match.Groups["constant"].Success)
            {
                int constantValue = int.Parse(match.Groups["constant"].Value);
                return Result<(int, int, int, bool)>.Success((0, 0, constantValue, true));
            }

            // Parse dice notation
            var countStr = match.Groups["count"].Value;
            var sidesStr = match.Groups["sides"].Value;
            var modifierStr = match.Groups["modifier"].Value;

            int diceCount = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);
            int dieSides = int.Parse(sidesStr);
            int modifier = string.IsNullOrEmpty(modifierStr) ? 0 : int.Parse(modifierStr);

            return Result<(int, int, int, bool)>.Success((diceCount, dieSides, modifier, false));
        }

        /// <summary>
        /// Rolls the specified number of dice with the specified number of sides
        /// </summary>
        /// <param name="count">Number of dice to roll</param>
        /// <param name="sides">Number of sides on each die</param>
        /// <returns>List of individual roll results</returns>
        private List<int> RollDice(int count, int sides)
        {
            var rolls = new List<int>(count);
            
            for (int i = 0; i < count; i++)
            {
                // Random.Next(min, max) where max is exclusive, so we use sides + 1
                int roll = _random.Next(1, sides + 1);
                rolls.Add(roll);
            }

            return rolls;
        }

        #endregion
    }
}
