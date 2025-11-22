using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class CombatEventTests
    {
        [Fact]
        public void TurnManager_Should_Raise_TurnChanged_Event()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            var turnManager = new StandardTurnManager(diceRoller);
            var creature = new StandardCreature("Hero", new StandardAbilityScores(), new StandardHitPoints(10));
            
            diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Normal)));
            turnManager.StartCombat(new[] { creature });

            using var monitoredTurnManager = turnManager.Monitor();

            // Act
            turnManager.NextTurn(); // Should cycle back to Hero (since only 1) and raise event

            // Assert
            monitoredTurnManager.Should().Raise(nameof(ITurnManager.TurnChanged))
                .WithArgs<TurnChangedEventArgs>(args => args.Creature == creature && args.Round == 2);
        }

        [Fact]
        public void HitPoints_Should_Raise_DamageTaken_Event()
        {
            // Arrange
            var hp = new StandardHitPoints(10);
            using var monitoredHp = hp.Monitor();

            // Act
            hp.TakeDamage(3);

            // Assert
            monitoredHp.Should().Raise(nameof(IHitPoints.DamageTaken))
                .WithArgs<DamageTakenEventArgs>(args => args.Amount == 3 && args.CurrentHealth == 7);
        }

        [Fact]
        public void HitPoints_Should_Raise_Died_Event()
        {
            // Arrange
            var hp = new StandardHitPoints(10);
            using var monitoredHp = hp.Monitor();

            // Act
            hp.TakeDamage(10);

            // Assert
            monitoredHp.Should().Raise(nameof(IHitPoints.Died));
        }
    }
}
