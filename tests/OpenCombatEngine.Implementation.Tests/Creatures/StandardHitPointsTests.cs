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

        [Fact]
        public void TakeDamage_Should_Fire_Downed_Not_Died_When_Dropping_To_Zero()
        {
            var hp = new StandardHitPoints(10, 10, 0);
            int downedCount = 0;
            int diedCount = 0;
            hp.Downed += (s, e) => downedCount++;
            hp.Died += (s, e) => diedCount++;

            hp.TakeDamage(10);

            hp.Current.Should().Be(0);
            downedCount.Should().Be(1);
            diedCount.Should().Be(0);
        }

        [Fact]
        public void TakeDamage_Should_Not_ReFire_Downed_On_Subsequent_Hits_At_Zero()
        {
            var hp = new StandardHitPoints(10, 10, 0);
            int downedCount = 0;
            hp.Downed += (s, e) => downedCount++;

            hp.TakeDamage(10); // Drops to 0 -> fires once
            hp.TakeDamage(5);  // Already at 0 -> should not fire again
            hp.TakeDamage(5);  // Still at 0 -> should not fire again

            downedCount.Should().Be(1);
        }

        [Fact]
        public void Died_Should_Only_Fire_After_Three_Failed_Death_Saves()
        {
            var hp = new StandardHitPoints(10, 10, 0);
            int diedCount = 0;
            hp.Died += (s, e) => diedCount++;

            hp.TakeDamage(10);
            diedCount.Should().Be(0);

            hp.RecordDeathSave(false);
            hp.RecordDeathSave(false);
            diedCount.Should().Be(0);

            hp.RecordDeathSave(false);
            diedCount.Should().Be(1);
            hp.IsDead.Should().BeTrue();
        }
    }
}
