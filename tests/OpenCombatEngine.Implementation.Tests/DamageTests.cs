using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Creatures;
using System.Collections.Generic;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class DamageTests
    {
        [Fact]
        public void Resistance_Should_Halve_Damage()
        {
            var stats = new StandardCombatStats(creature: null!, 
                resistances: new[] { DamageType.Fire }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(10, DamageType.Fire);
            
            hp.Current.Should().Be(15); // 20 - (10/2) = 15
        }

        [Fact]
        public void Vulnerability_Should_Double_Damage()
        {
            var stats = new StandardCombatStats(creature: null!,
                vulnerabilities: new[] { DamageType.Cold }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(5, DamageType.Cold);
            
            hp.Current.Should().Be(10); // 20 - (5*2) = 10
        }

        [Fact]
        public void Immunity_Should_Prevent_Damage()
        {
            var stats = new StandardCombatStats(creature: null!,
                immunities: new[] { DamageType.Poison }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(100, DamageType.Poison);
            
            hp.Current.Should().Be(20);
        }

        [Fact]
        public void TakeDamage_Should_Reduce_Current_HP()
        {
            // Arrange
            var hp = new StandardHitPoints(10, new StandardCombatStats(creature: null!));

            // Act
            hp.TakeDamage(4);

            // Assert
            hp.Current.Should().Be(6);
        }

        [Fact]
        public void TakeDamage_Should_Not_Go_Below_Zero()
        {
            // Arrange
            var hp = new StandardHitPoints(10, new StandardCombatStats(creature: null!));

            // Act
            hp.TakeDamage(15);

            // Assert
            hp.Current.Should().Be(0);
        }

        [Fact]
        public void TakeDamage_Should_Reduce_Temporary_HP_First()
        {
            // Arrange
            var hp = new StandardHitPoints(10, 10, 5, new StandardCombatStats(creature: null!));

            // Act
            hp.TakeDamage(4);

            // Assert
            hp.Temporary.Should().Be(1);
            hp.Current.Should().Be(10);
        }

        [Fact]
        public void TakeDamage_Should_Spill_Over_Temporary_HP()
        {
            // Arrange
            var hp = new StandardHitPoints(10, 10, 5, new StandardCombatStats(creature: null!));

            // Act
            hp.TakeDamage(8);

            // Assert
            hp.Temporary.Should().Be(0);
            hp.Current.Should().Be(7);
        }

        [Fact]
        public void Unspecified_Damage_Should_Ignore_Resistances()
        {
            var stats = new StandardCombatStats(creature: null!,
                resistances: new[] { DamageType.Fire }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(10, DamageType.Unspecified);
            
            hp.Current.Should().Be(10);
        }
    }
}
