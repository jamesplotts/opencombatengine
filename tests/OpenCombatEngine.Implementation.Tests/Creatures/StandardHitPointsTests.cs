using FluentAssertions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class StandardHitPointsTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties_Default()
        {
            // Arrange
            var hp = new StandardHitPoints(max: 20);

            // Assert
            hp.Max.Should().Be(20);
            hp.Current.Should().Be(20);
            hp.Temporary.Should().Be(0);
            hp.IsDead.Should().BeFalse();
        }

        [Fact]
        public void Constructor_Should_Set_Properties_Custom()
        {
            // Arrange
            var hp = new StandardHitPoints(max: 20, current: 10, temporary: 5);

            // Assert
            hp.Max.Should().Be(20);
            hp.Current.Should().Be(10);
            hp.Temporary.Should().Be(5);
            hp.IsDead.Should().BeFalse();
        }

        [Fact]
        public void Constructor_Should_Clamp_Current_To_Max()
        {
            // Arrange
            var hp = new StandardHitPoints(max: 20, current: 30);

            // Assert
            hp.Current.Should().Be(20);
        }

        [Fact]
        public void Constructor_Should_Clamp_Current_To_Zero()
        {
            // Arrange
            var hp = new StandardHitPoints(max: 20, current: -5);

            // Assert
            hp.Current.Should().Be(0);
            hp.IsDead.Should().BeTrue();
        }

        [Fact]
        public void IsDead_Should_Be_True_When_Current_Is_Zero()
        {
            // Arrange
            var hp = new StandardHitPoints(max: 20, current: 0);

            // Assert
            hp.IsDead.Should().BeTrue();
        }

        [Fact]
        public void Constructor_Should_Throw_On_Invalid_Max()
        {
            // Act
            Action act = () => new StandardHitPoints(max: 0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage("*must be positive*");
        }
    }
}
