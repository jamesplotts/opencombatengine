using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    public class AttackActionTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _source;
        private readonly ICreature _target;

        public AttackActionTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            
            // Setup Source
            _source = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Attacker",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardCombatStats()
            );

            // Setup Target with AC 15, HP 20
            _target = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Defender",
                new StandardAbilityScores(),
                new StandardHitPoints(20),
                new StandardCombatStats(armorClass: 15)
            );
        }

        [Fact]
        public void Execute_Should_Hit_When_Roll_Meets_AC()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", attackBonus: 5, damageDice: "1d8", _diceRoller);
            
            // Mock Attack Roll: 10 + 5 = 15 (Hits AC 15)
            _diceRoller.Roll("1d20+5").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(15, "1d20+5", new List<int> { 10 }, 5, RollType.Normal)));

            // Mock Damage Roll: 6
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            // Act
            var result = action.Execute(_source, _target);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Success.Should().BeTrue();
            result.Value.DamageDealt.Should().Be(6);
            result.Value.Message.Should().Contain("Hit!");
            
            _target.HitPoints.Current.Should().Be(14); // 20 - 6
        }

        [Fact]
        public void Execute_Should_Miss_When_Roll_Below_AC()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", attackBonus: 5, damageDice: "1d8", _diceRoller);
            
            // Mock Attack Roll: 9 + 5 = 14 (Misses AC 15)
            _diceRoller.Roll("1d20+5").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(14, "1d20+5", new List<int> { 9 }, 5, RollType.Normal)));

            // Act
            var result = action.Execute(_source, _target);

            // Assert
            result.IsSuccess.Should().BeTrue(); // The *execution* succeeded (didn't crash)
            result.Value.Success.Should().BeFalse(); // The *attack* missed
            result.Value.DamageDealt.Should().Be(0);
            result.Value.Message.Should().Contain("Missed");
            
            _target.HitPoints.Current.Should().Be(20); // No damage
        }

        [Fact]
        public void Execute_Should_Fail_If_Dice_Roll_Fails()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", attackBonus: 5, damageDice: "1d8", _diceRoller);
            
            _diceRoller.Roll("1d20+5").Returns(Result<DiceRollResult>.Failure("Dice error"));

            // Act
            var result = action.Execute(_source, _target);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Dice error");
        }
    }
}
