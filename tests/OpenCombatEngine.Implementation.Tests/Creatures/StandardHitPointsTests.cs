using FluentAssertions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;
using System;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class StandardHitPointsTests
    {
        [Fact]
        public void Constructor_Should_Initialize_Properties()
        {
            var hp = new StandardHitPoints(10, 10, 0);
            hp.Max.Should().Be(10);
            hp.Current.Should().Be(10);
            hp.Temporary.Should().Be(0);
        }

        [Fact]
        public void TakeDamage_Should_Reduce_Current_HP()
        {
            var hp = new StandardHitPoints(10, 10, 0);
            hp.TakeDamage(5);
            hp.Current.Should().Be(5);
        }

        [Fact]
        public void TakeDamage_Should_Reduce_Temporary_HP_First()
        {
            var hp = new StandardHitPoints(10, 10, 5);
            hp.TakeDamage(3);
            hp.Temporary.Should().Be(2);
            hp.Current.Should().Be(10);

            hp.TakeDamage(5);
            hp.Temporary.Should().Be(0);
            hp.Current.Should().Be(7);
        }

        [Fact]
        public void Constructor_Should_Clamp_Current_To_Max()
        {
            // Arrange
            var hp = new StandardHitPoints(20, 30, 0);

            // Assert
            hp.Current.Should().Be(20);
        }

        [Fact]
        public void Constructor_Should_Clamp_Current_To_Zero()
        {
            // Arrange
            var hp = new StandardHitPoints(20, -5, 0);

            // Assert
            hp.Current.Should().Be(0);
            hp.IsDead.Should().BeFalse(); // 0 HP is unconscious, not dead
        }

        [Fact]
        public void IsDead_Should_Be_False_When_Current_Is_Zero()
        {
            // Arrange
            var hp = new StandardHitPoints(20, 0, 0);

            // Assert
            hp.IsDead.Should().BeFalse();
        }

        [Fact]
        public void Constructor_Should_Throw_On_Invalid_Max()
        {
            // Act
            Action act = () => new StandardHitPoints(0, 0, 0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage("*must be positive*");
        }
    }
}
