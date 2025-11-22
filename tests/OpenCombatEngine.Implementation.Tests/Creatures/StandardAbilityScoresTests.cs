using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class StandardAbilityScoresTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties()
        {
            // Arrange
            var scores = new StandardAbilityScores(
                strength: 15,
                dexterity: 14,
                constitution: 13,
                intelligence: 12,
                wisdom: 10,
                charisma: 8);

            // Assert
            scores.Strength.Should().Be(15);
            scores.Dexterity.Should().Be(14);
            scores.Constitution.Should().Be(13);
            scores.Intelligence.Should().Be(12);
            scores.Wisdom.Should().Be(10);
            scores.Charisma.Should().Be(8);
        }

        [Theory]
        [InlineData(1, -5)]
        [InlineData(2, -4)]
        [InlineData(3, -4)]
        [InlineData(4, -3)]
        [InlineData(5, -3)]
        [InlineData(6, -2)]
        [InlineData(7, -2)]
        [InlineData(8, -1)]
        [InlineData(9, -1)]
        [InlineData(10, 0)]
        [InlineData(11, 0)]
        [InlineData(12, 1)]
        [InlineData(13, 1)]
        [InlineData(14, 2)]
        [InlineData(15, 2)]
        [InlineData(16, 3)]
        [InlineData(17, 3)]
        [InlineData(18, 4)]
        [InlineData(19, 4)]
        [InlineData(20, 5)]
        [InlineData(30, 10)]
        public void GetModifier_Should_Calculate_Correctly(int score, int expectedModifier)
        {
            // Arrange
            // We only care about Strength for this test as the logic is shared
            var scores = new StandardAbilityScores(strength: score);

            // Act
            var modifier = scores.GetModifier(Ability.Strength);

            // Assert
            modifier.Should().Be(expectedModifier);
        }

        [Fact]
        public void Constructor_Should_Throw_On_Negative_Score()
        {
            // Act
            Action act = () => new StandardAbilityScores(strength: -1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage("*cannot be negative*");
        }
    }
}
