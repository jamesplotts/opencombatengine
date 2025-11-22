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
            var stats = new StandardCombatStats(
                resistances: new[] { DamageType.Fire }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(10, DamageType.Fire);
            
            hp.Current.Should().Be(15); // 20 - (10/2) = 15
        }

        [Fact]
        public void Vulnerability_Should_Double_Damage()
        {
            var stats = new StandardCombatStats(
                vulnerabilities: new[] { DamageType.Cold }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(5, DamageType.Cold);
            
            hp.Current.Should().Be(10); // 20 - (5*2) = 10
        }

        [Fact]
        public void Immunity_Should_Prevent_Damage()
        {
            var stats = new StandardCombatStats(
                immunities: new[] { DamageType.Poison }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(100, DamageType.Poison);
            
            hp.Current.Should().Be(20);
        }

        [Fact]
        public void Unspecified_Damage_Should_Ignore_Resistances()
        {
            var stats = new StandardCombatStats(
                resistances: new[] { DamageType.Fire }
            );
            
            var hp = new StandardHitPoints(20, combatStats: stats);
            
            hp.TakeDamage(10, DamageType.Unspecified);
            
            hp.Current.Should().Be(10);
        }
    }
}
