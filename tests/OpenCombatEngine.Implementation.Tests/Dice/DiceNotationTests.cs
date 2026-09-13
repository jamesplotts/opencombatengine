using FluentAssertions;
using OpenCombatEngine.Implementation.Dice;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Dice
{
    /// <summary>
    /// Tests for <see cref="DiceNotation"/> — see its own doc comment for
    /// the real bug this exists to prevent: naively string-interpolating
    /// a negative modifier produces a double sign ("1d20+-1") that
    /// StandardDiceRoller's own notation regex rejects outright.
    /// </summary>
    public class DiceNotationTests
    {
        [Fact]
        public void WithModifier_PositiveModifier_PrependsPlusSign()
        {
            DiceNotation.WithModifier("1d20", 3).Should().Be("1d20+3");
        }

        [Fact]
        public void WithModifier_ZeroModifier_PrependsPlusSign()
        {
            DiceNotation.WithModifier("1d20", 0).Should().Be("1d20+0");
        }

        [Fact]
        public void WithModifier_NegativeModifier_UsesSingleMinusSign()
        {
            // The regression case itself: must be "1d20-1", never the
            // double-signed "1d20+-1" naive interpolation used to produce.
            DiceNotation.WithModifier("1d20", -1).Should().Be("1d20-1");
        }

        [Fact]
        public void WithModifier_LargeNegativeModifier_UsesSingleMinusSign()
        {
            DiceNotation.WithModifier("1d20", -12).Should().Be("1d20-12");
        }
    }
}
