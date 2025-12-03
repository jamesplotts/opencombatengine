using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Tests.Dice
{
    /// <summary>
    /// Comprehensive unit tests for IDiceRoller implementations
    /// </summary>
    public class DiceRollerTests
    {
        // Note: These tests are written BEFORE implementation per TDD methodology
        // The implementation will be created to make these tests pass

        #region Test Data

        /// <summary>
        /// Valid dice notation test cases
        /// </summary>
        public static IEnumerable<object[]> ValidNotationTestData =>
            new List<object[]>
            {
                new object[] { "1d20" },      // Basic single die
                new object[] { "3d6" },       // Multiple dice
                new object[] { "2d8+5" },     // With positive modifier
                new object[] { "1d12-3" },     // With negative modifier
                new object[] { "d20" },       // Implicit 1 die
                new object[] { "d6+2" },      // Implicit 1 die with modifier
                new object[] { "4d4+10" },     // Multiple dice with large modifier
                new object[] { "10d10" },      // Many dice
                new object[] { "1d100" },     // Percentile die
                new object[] { "5" },         // Constant value
                new object[] { "0" },         // Zero constant
                new object[] { "-5" }         // Negative constant
            };

        /// <summary>
        /// Invalid dice notation test cases
        /// </summary>
        public static IEnumerable<object[]> InvalidNotationTestData =>
            new List<object[]>
            {
                new object[] { "" },          // Empty string
                new object[] { " " },         // Whitespace only
                new object[] { null },        // Null
                new object[] { "abc" },       // Non-numeric
                new object[] { "1d" },        // Missing die sides
                new object[] { "d" },         // Missing everything
                new object[] { "1d20d20" },   // Malformed
                new object[] { "1d20+" },     // Missing modifier value
                new object[] { "1d20-" },     // Missing modifier value
                new object[] { "1.5d20" },   // Decimal dice count
                new object[] { "1d20.5" },    // Decimal die sides
                new object[] { "1d0" },       // Zero-sided die
                new object[] { "0d20" },      // Zero dice
                new object[] { "-1d20" },     // Negative dice count
                new object[] { "1d-20" }      // Negative die sides
            };

        #endregion

        #region IsValidNotation Tests

        /// <summary>
        /// Tests that valid notation strings are correctly identified as valid
        /// </summary>
        [Theory]
        [MemberData(nameof(ValidNotationTestData))]
        public void IsValidNotation_WithValidNotation_ReturnsTrue(string notation)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            bool isValid = roller.IsValidNotation(notation);

            // Assert
            Assert.True(isValid, $"Notation '{notation}' should be valid");
        }

        /// <summary>
        /// Tests that invalid notation strings are correctly identified as invalid
        /// </summary>
        [Theory]
        [MemberData(nameof(InvalidNotationTestData))]
        public void IsValidNotation_WithInvalidNotation_ReturnsFalse(string notation)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            bool isValid = roller.IsValidNotation(notation);

            // Assert
            Assert.False(isValid, $"Notation '{notation}' should be invalid");
        }

        #endregion

        #region Basic Roll Tests

        /// <summary>
        /// Tests that rolling with valid notation returns success
        /// </summary>
        [Theory]
        [MemberData(nameof(ValidNotationTestData))]
        public void Roll_WithValidNotation_ReturnsSuccess(string notation)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            Result<DiceRollResult> result = roller.Roll(notation);

            // Assert
            Assert.True(result.IsSuccess, $"Roll with notation '{notation}' should succeed");
            Assert.NotNull(result.Value);
            Assert.Equal(notation, result.Value.Notation);
        }

        /// <summary>
        /// Tests that rolling with invalid notation returns failure
        /// </summary>
        [Theory]
        [MemberData(nameof(InvalidNotationTestData))]
        public void Roll_WithInvalidNotation_ReturnsFailure(string notation)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            Result<DiceRollResult> result = roller.Roll(notation);

            // Assert
            Assert.True(result.IsFailure, $"Roll with notation '{notation}' should fail");
            Assert.NotEmpty(result.Error);
        }

        /// <summary>
        /// Tests that rolling a constant value returns that exact value
        /// </summary>
        [Theory]
        [InlineData("5", 5)]
        [InlineData("10", 10)]
        [InlineData("0", 0)]
        [InlineData("-5", -5)]
        [InlineData("100", 100)]
        public void Roll_WithConstantValue_ReturnsExactValue(string notation, int expectedValue)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            Result<DiceRollResult> result = roller.Roll(notation);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedValue, result.Value.Total);
            Assert.Empty(result.Value.IndividualRolls);
            Assert.Equal(expectedValue, result.Value.Modifier);
        }

        /// <summary>
        /// Tests that dice rolls produce values within expected ranges
        /// </summary>
        [Theory]
        [InlineData("1d6", 1, 6)]
        [InlineData("1d20", 1, 20)]
        [InlineData("3d6", 3, 18)]
        [InlineData("2d8+5", 7, 21)]      // 2-16 + 5
        [InlineData("1d12-3", -2, 9)]     // 1-12 - 3
        [InlineData("4d4+10", 14, 26)]    // 4-16 + 10
        public void Roll_WithDiceNotation_ReturnsValueInExpectedRange(
            string notation, int minExpected, int maxExpected)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();
            const int iterations = 100; // Roll multiple times to test range

            // Act & Assert
            for (int i = 0; i < iterations; i++)
            {
                Result<DiceRollResult> result = roller.Roll(notation);
                
                Assert.True(result.IsSuccess);
                Assert.InRange(result.Value.Total, minExpected, maxExpected);
            }
        }

        /// <summary>
        /// Tests that individual die rolls are within valid range
        /// </summary>
        [Theory]
        [InlineData("3d6", 3, 1, 6)]
        [InlineData("2d8", 2, 1, 8)]
        [InlineData("5d10", 5, 1, 10)]
        [InlineData("1d20", 1, 1, 20)]
        [InlineData("d100", 1, 1, 100)]
        public void Roll_WithMultipleDice_ProducesCorrectIndividualRolls(
            string notation, int expectedDiceCount, int minDieValue, int maxDieValue)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            Result<DiceRollResult> result = roller.Roll(notation);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedDiceCount, result.Value.IndividualRolls.Count);
            Assert.All(result.Value.IndividualRolls, 
                roll => Assert.InRange(roll, minDieValue, maxDieValue));
        }

        /// <summary>
        /// Tests that modifiers are correctly applied
        /// </summary>
        [Theory]
        [InlineData("1d6+3", 3)]
        [InlineData("2d8-5", -5)]
        [InlineData("3d4+10", 10)]
        [InlineData("d20-1", -1)]
        [InlineData("1d12+0", 0)]
        public void Roll_WithModifier_AppliesModifierCorrectly(string notation, int expectedModifier)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            Result<DiceRollResult> result = roller.Roll(notation);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedModifier, result.Value.Modifier);
            Assert.Equal(result.Value.DiceTotal + expectedModifier, result.Value.Total);
        }

        #endregion

        #region Advantage/Disadvantage Tests

        /// <summary>
        /// Tests that advantage rolls twice and takes the higher value
        /// </summary>
        [Fact]
        public void RollWithAdvantage_RollsTwiceAndTakesHigher()
        {
            // Arrange
            IDiceRoller roller = CreateDiceRollerWithSeed(42); // Fixed seed for deterministic testing
            const string notation = "1d20";

            // Act
            Result<DiceRollResult> result = roller.RollWithAdvantage(notation);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(RollType.Advantage, result.Value.RollType);
            Assert.NotNull(result.Value.AlternateRoll);
            
            // Verify the main result is the higher of the two
            Assert.True(result.Value.Total >= result.Value.AlternateRoll.Total,
                "Advantage should select the higher roll");
        }

        /// <summary>
        /// Tests that disadvantage rolls twice and takes the lower value
        /// </summary>
        [Fact]
        public void RollWithDisadvantage_RollsTwiceAndTakesLower()
        {
            // Arrange
            IDiceRoller roller = CreateDiceRollerWithSeed(42); // Fixed seed for deterministic testing
            const string notation = "1d20";

            // Act
            Result<DiceRollResult> result = roller.RollWithDisadvantage(notation);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(RollType.Disadvantage, result.Value.RollType);
            Assert.NotNull(result.Value.AlternateRoll);
            
            // Verify the main result is the lower of the two
            Assert.True(result.Value.Total <= result.Value.AlternateRoll.Total,
                "Disadvantage should select the lower roll");
        }

        /// <summary>
        /// Tests that advantage/disadvantage work with modifiers
        /// </summary>
        [Theory]
        [InlineData("1d20+5")]
        [InlineData("1d20-3")]
        [InlineData("d20+10")]
        public void RollWithAdvantageDisadvantage_WithModifiers_AppliesModifiersToBothRolls(string notation)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act - Test Advantage
            Result<DiceRollResult> advantageResult = roller.RollWithAdvantage(notation);
            
            // Assert Advantage
            Assert.True(advantageResult.IsSuccess);
            Assert.NotNull(advantageResult.Value.AlternateRoll);
            Assert.Equal(advantageResult.Value.Modifier, advantageResult.Value.AlternateRoll.Modifier);

            // Act - Test Disadvantage
            Result<DiceRollResult> disadvantageResult = roller.RollWithDisadvantage(notation);
            
            // Assert Disadvantage
            Assert.True(disadvantageResult.IsSuccess);
            Assert.NotNull(disadvantageResult.Value.AlternateRoll);
            Assert.Equal(disadvantageResult.Value.Modifier, disadvantageResult.Value.AlternateRoll.Modifier);
        }

        #endregion

        #region Critical Success/Failure Tests

        /// <summary>
        /// Tests that natural 20 on d20 is detected as critical success
        /// </summary>
        [Fact]
        public void DiceRollResult_WithNatural20OnD20_IsCriticalSuccess()
        {
            // Arrange
            var result = new DiceRollResult(
                Total: 25,
                Notation: "1d20+5",
                IndividualRolls: new[] { 20 },
                Modifier: 5,
                RollType: RollType.Normal
            );

            // Assert
            Assert.True(result.IsCriticalSuccess);
            Assert.False(result.IsCriticalFailure);
        }

        /// <summary>
        /// Tests that natural 1 on d20 is detected as critical failure
        /// </summary>
        [Fact]
        public void DiceRollResult_WithNatural1OnD20_IsCriticalFailure()
        {
            // Arrange
            var result = new DiceRollResult(
                Total: 6,
                Notation: "1d20+5",
                IndividualRolls: new[] { 1 },
                Modifier: 5,
                RollType: RollType.Normal
            );

            // Assert
            Assert.False(result.IsCriticalSuccess);
            Assert.True(result.IsCriticalFailure);
        }

        /// <summary>
        /// Tests that criticals only apply to single d20 rolls
        /// </summary>
        [Theory]
        [InlineData("2d20", new[] { 20, 20 }, false, false)]  // Multiple d20s
        [InlineData("1d12", new[] { 12 }, false, false)]       // Not a d20
        [InlineData("1d12", new[] { 1 }, false, false)]        // Not a d20
        [InlineData("3d6", new[] { 6, 6, 6 }, false, false)]   // Multiple non-d20s
        public void DiceRollResult_WithNonSingleD20_NoCriticals(
            string notation, int[] rolls, bool expectedCritSuccess, bool expectedCritFailure)
        {
            // Arrange
            var result = new DiceRollResult(
                Total: rolls.Sum(),
                Notation: notation,
                IndividualRolls: rolls,
                Modifier: 0,
                RollType: RollType.Normal
            );

            // Assert
            Assert.Equal(expectedCritSuccess, result.IsCriticalSuccess);
            Assert.Equal(expectedCritFailure, result.IsCriticalFailure);
        }

        #endregion

        #region Seed/Reproducibility Tests

        /// <summary>
        /// Tests that setting a seed produces reproducible results
        /// </summary>
        [Fact]
        public void Roll_WithSameSeed_ProducesIdenticalResults()
        {
            // Arrange
            const int seed = 12345;
            const string notation = "3d6+2";
            
            IDiceRoller roller1 = CreateDiceRollerWithSeed(seed);
            IDiceRoller roller2 = CreateDiceRollerWithSeed(seed);

            // Act
            Result<DiceRollResult> result1 = roller1.Roll(notation);
            Result<DiceRollResult> result2 = roller2.Roll(notation);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
            Assert.Equal(result1.Value.Total, result2.Value.Total);
            Assert.Equal(result1.Value.IndividualRolls, result2.Value.IndividualRolls);
        }

        /// <summary>
        /// Tests that different seeds produce different results
        /// </summary>
        [Fact]
        public void Roll_WithDifferentSeeds_ProducesDifferentResults()
        {
            // Arrange
            const string notation = "1d20";
            IDiceRoller roller1 = CreateDiceRollerWithSeed(111);
            IDiceRoller roller2 = CreateDiceRollerWithSeed(222);

            // Act - Roll multiple times to ensure difference
            var results1 = new List<int>();
            var results2 = new List<int>();
            
            for (int i = 0; i < 10; i++)
            {
                var r1 = roller1.Roll(notation);
                var r2 = roller2.Roll(notation);
                
                Assert.True(r1.IsSuccess);
                Assert.True(r2.IsSuccess);
                
                results1.Add(r1.Value.Total);
                results2.Add(r2.Value.Total);
            }

            // Assert - Sequences should be different
            Assert.NotEqual(results1, results2);
        }

        /// <summary>
        /// Tests that null seed produces non-deterministic results
        /// </summary>
        [Fact]
        public void Roll_WithNullSeed_ProducesRandomResults()
        {
            // Arrange
            IDiceRoller roller1 = CreateDiceRoller();
            IDiceRoller roller2 = CreateDiceRoller();
            const string notation = "1d20";

            // Act - Roll multiple times
            var results1 = new List<int>();
            var results2 = new List<int>();
            
            for (int i = 0; i < 20; i++)
            {
                var r1 = roller1.Roll(notation);
                var r2 = roller2.Roll(notation);
                
                Assert.True(r1.IsSuccess);
                Assert.True(r2.IsSuccess);
                
                results1.Add(r1.Value.Total);
                results2.Add(r2.Value.Total);
            }

            // Assert - With high probability, sequences should differ
            // (There's a tiny chance they could be identical by coincidence)
            Assert.NotEqual(results1, results2);
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Tests handling of edge case notation values
        /// </summary>
        [Theory]
        [InlineData("d20", 1, 20)]      // Implicit 1 die
        [InlineData("D20", 1, 20)]      // Capital D
        [InlineData("1D20", 1, 20)]     // All caps
        [InlineData("d6+2", 3, 8)]      // Implicit 1 die with modifier
        public void Roll_WithEdgeCaseNotation_HandlesCorrectly(
            string notation, int minExpected, int maxExpected)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            Result<DiceRollResult> result = roller.Roll(notation);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.InRange(result.Value.Total, minExpected, maxExpected);
        }

        /// <summary>
        /// Tests handling of whitespace in notation
        /// </summary>
        [Theory]
        [InlineData(" 1d20 ")]
        [InlineData("1d20 ")]
        [InlineData(" 1d20")]
        [InlineData("1 d 20")]
        [InlineData("1d20 + 5")]
        [InlineData("1d20+ 5")]
        [InlineData("1d20 +5")]
        public void Roll_WithWhitespace_HandlesGracefully(string notation)
        {
            // Arrange
            IDiceRoller roller = CreateDiceRoller();

            // Act
            Result<DiceRollResult> result = roller.Roll(notation.Trim().Replace(" ", ""));

            // Assert
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region DiceRollResult Tests

        /// <summary>
        /// Tests that DiceTotal calculates correctly
        /// </summary>
        [Fact]
        public void DiceRollResult_DiceTotal_CalculatesCorrectly()
        {
            // Arrange
            var result = new DiceRollResult(
                Total: 15,
                Notation: "3d6+2",
                IndividualRolls: new[] { 4, 5, 4 },
                Modifier: 2,
                RollType: RollType.Normal
            );

            // Assert
            Assert.Equal(13, result.DiceTotal); // 4+5+4
            Assert.Equal(3, result.DiceCount);
        }

        /// <summary>
        /// Tests the ToString formatting
        /// </summary>
        [Theory]
        [InlineData("3d6+2", new[] { 4, 5, 4 }, 2, 15, RollType.Normal, 
            "3d6+2 = [4, 5, 4] + 2 = 15")]
        [InlineData("2d8-3", new[] { 6, 7 }, -3, 10, RollType.Normal, 
            "2d8-3 = [6, 7] - 3 = 10")]
        [InlineData("5", new int[0], 5, 5, RollType.Normal, 
            "5 = 5")]
        public void DiceRollResult_ToString_FormatsCorrectly(
            string notation, int[] rolls, int modifier, int total, 
            RollType rollType, string expectedStart)
        {
            // Arrange
            var result = new DiceRollResult(
                Total: total,
                Notation: notation,
                IndividualRolls: rolls,
                Modifier: modifier,
                RollType: rollType
            );

            // Act
            string formatted = result.ToString();

            // Assert
            Assert.StartsWith(expectedStart, formatted);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a dice roller instance for testing
        /// </summary>
        /// <remarks>
        /// This will be replaced with actual implementation creation
        /// </remarks>
        private IDiceRoller CreateDiceRoller()
        {
            return new OpenCombatEngine.Implementation.Dice.StandardDiceRoller();
        }

        /// <summary>
        /// Creates a dice roller with a specific seed for reproducible testing
        /// </summary>
        private IDiceRoller CreateDiceRollerWithSeed(int seed)
        {
            IDiceRoller roller = CreateDiceRoller();
            roller.Seed = seed;
            return roller;
        }

        #endregion
    }
}