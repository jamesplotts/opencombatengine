using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;
using System.Collections.Generic;

namespace OpenCombatEngine.Implementation.Tests.Combat
{
    public class EnvironmentalEffectsTests
    {
        [Fact]
        public void ResolveAttack_Should_Apply_Half_Cover_Bonus()
        {
            // Arrange
            var attacker = Substitute.For<ICreature>();
            var target = new StandardCreature(System.Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            // Base AC is 10 + Dex(0) = 10.
            
            // Attack roll of 11 hits AC 10.
            // With Half Cover (+2 AC), AC becomes 12. Attack of 11 should miss.
            
            var attack = new AttackResult(
                attacker, 
                target, 
                11, 
                false, 
                false, 
                false, 
                new List<DamageRoll>(),
                targetCover: CoverType.Half
            );

            // Act
            var outcome = target.ResolveAttack(attack);

            // Assert
            outcome.IsHit.Should().BeFalse("Attack of 11 should miss AC 12 (10 + 2 Cover)");
        }

        [Fact]
        public void ResolveAttack_Should_Apply_ThreeQuarters_Cover_Bonus()
        {
            // Arrange
            var attacker = Substitute.For<ICreature>();
            var target = new StandardCreature(System.Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            // Base AC 10.
            
            // Attack roll of 14 hits AC 10.
            // With 3/4 Cover (+5 AC), AC becomes 15. Attack of 14 should miss.
            
            var attack = new AttackResult(
                attacker, 
                target, 
                14, 
                false, 
                false, 
                false, 
                new List<DamageRoll>(),
                targetCover: CoverType.ThreeQuarters
            );

            // Act
            var outcome = target.ResolveAttack(attack);

            // Assert
            outcome.IsHit.Should().BeFalse("Attack of 14 should miss AC 15 (10 + 5 Cover)");
        }

        [Fact]
        public void ResolveAttack_Should_Fail_Against_Total_Cover()
        {
            // Arrange
            var attacker = Substitute.For<ICreature>();
            var target = new StandardCreature(System.Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            
            // Attack roll of 30 (Critical!)
            // Total Cover prevents attack entirely.
            
            var attack = new AttackResult(
                attacker, 
                target, 
                30, 
                true, // Critical
                false, 
                false, 
                new List<DamageRoll>(),
                targetCover: CoverType.Total
            );

            // Act
            var outcome = target.ResolveAttack(attack);

            // Assert
            outcome.IsHit.Should().BeFalse("Total Cover should prevent hit even on critical");
            outcome.Message.Should().Contain("Total Cover");
        }

        [Fact]
        public void ResolveAttack_Should_Hit_If_Roll_Exceeds_Cover_AC()
        {
            // Arrange
            var attacker = Substitute.For<ICreature>();
            var target = new StandardCreature(System.Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            // Base AC 10. Half Cover (+2) = 12.
            
            var attack = new AttackResult(
                attacker, 
                target, 
                12, 
                false, 
                false, 
                false, 
                new List<DamageRoll>(),
                targetCover: CoverType.Half
            );

            // Act
            var outcome = target.ResolveAttack(attack);

            // Assert
            outcome.IsHit.Should().BeTrue("Attack of 12 should hit AC 12");
        }
    }
}
