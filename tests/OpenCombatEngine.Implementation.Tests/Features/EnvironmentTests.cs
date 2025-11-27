using System.Collections.Generic;
using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items; // Added
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class EnvironmentTests
    {
        private readonly StandardCreature _attacker;
        private readonly StandardCreature _target;

        public EnvironmentTests()
        {
            _attacker = new StandardCreature("11111111-1111-1111-1111-111111111111", "Attacker", new StandardAbilityScores(), new StandardHitPoints(10, new StandardCombatStats()), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            _target = new StandardCreature("22222222-2222-2222-2222-222222222222", "Target", new StandardAbilityScores(), new StandardHitPoints(10, new StandardCombatStats()), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
        }

        [Theory]
        [InlineData(CoverType.None, 10, true)] // AC 10, Roll 10 -> Hit
        [InlineData(CoverType.Half, 11, false)] // AC 10+2=12, Roll 11 -> Miss
        [InlineData(CoverType.Half, 12, true)] // AC 10+2=12, Roll 12 -> Hit
        [InlineData(CoverType.ThreeQuarters, 14, false)] // AC 10+5=15, Roll 14 -> Miss
        [InlineData(CoverType.ThreeQuarters, 15, true)] // AC 10+5=15, Roll 15 -> Hit
        public void Cover_Should_Increase_AC(CoverType cover, int roll, bool expectedHit)
        {
            // Arrange
            // Target Base AC is 10 (10 + Dex 0)
            var attack = new AttackResult(
                _attacker,
                _target,
                roll,
                false, // isCritical
                false, // hasAdvantage
                false, // hasDisadvantage
                new List<DamageRoll> { new DamageRoll(1, DamageType.Slashing) },
                cover,
                ObscurementType.None
            );

            // Act
            var outcome = _target.ResolveAttack(attack);

            // Assert
            outcome.IsHit.Should().Be(expectedHit);
        }

        [Fact]
        public void Total_Cover_Should_Prevent_Hit()
        {
            // Arrange
            var attack = new AttackResult(
                _attacker,
                _target,
                20, // High roll
                true, // isCritical
                false, // hasAdvantage
                false, // hasDisadvantage
                new List<DamageRoll> { new DamageRoll(1, DamageType.Slashing) },
                CoverType.Total,
                ObscurementType.None
            );

            // Act
            var outcome = _target.ResolveAttack(attack);

            // Assert
            outcome.IsHit.Should().BeFalse();
            outcome.Message.Should().Contain("Total Cover");
        }

        [Fact]
        public void Difficult_Terrain_Should_Double_Movement_Cost()
        {
            // Arrange
            _attacker.Movement.IsInDifficultTerrain = true;
            int initialSpeed = _attacker.Movement.Speed; // 30
            
            // Act
            _attacker.Movement.Move(5); // Should cost 10

            // Assert
            _attacker.Movement.MovementRemaining.Should().Be(initialSpeed - 10);
        }

        [Fact]
        public void Difficult_Terrain_Should_Prevent_Movement_If_Cost_Exceeds_Remaining()
        {
            // Arrange
            _attacker.Movement.IsInDifficultTerrain = true;
            // Speed 30. Move 20 costs 40 -> Fail.
            
            // Act
            System.Action act = () => _attacker.Movement.Move(20);

            // Assert
            act.Should().Throw<System.InvalidOperationException>()
                .WithMessage("*Not enough movement*");
        }
    }
}
