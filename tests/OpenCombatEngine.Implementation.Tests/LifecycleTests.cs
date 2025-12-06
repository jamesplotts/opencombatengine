using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class LifecycleTests
    {
        [Fact]
        public void StartCombat_Should_Call_StartTurn_On_FirstCreature()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            var turnManager = new StandardTurnManager(diceRoller);
            
            var creature = Substitute.For<ICreature>();
            creature.CombatStats.Returns(new StandardCombatStats(creature: null!));
            creature.AbilityScores.Returns(new StandardAbilityScores());

            diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Normal)));
            
            // Act
            turnManager.StartCombat(new[] { creature });

            // Assert
            creature.Received(1).StartTurn();
        }

        [Fact]
        public void StartTurn_Should_Tick_Conditions()
        {
            // Arrange
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Hero", new StandardAbilityScores(), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            var condition = Substitute.For<ICondition>();
            condition.Name.Returns("TestCondition");
            condition.DurationRounds.Returns(1);

            creature.Conditions.AddCondition(condition);

            // Act
            creature.StartTurn();

            // Assert
            condition.Received(1).OnTurnStart(creature);
        }
        
        [Fact]
        public void Integration_TurnManager_Should_Tick_Conditions()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            var turnManager = new StandardTurnManager(diceRoller);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Hero", new StandardAbilityScores(), new StandardHitPoints(10), new StandardInventory(), turnManager);
            
            // Condition with 2 round duration. 
            // StartCombat (Turn 1) -> Tick -> Duration 1.
            var condition = new Condition("Buff", "Lasts 2 rounds", 2);
            creature.Conditions.AddCondition(condition);

            diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Normal)));
            
            // Act
            turnManager.StartCombat(new[] { creature });

            // Assert
            creature.Conditions.ActiveConditions.Should().Contain(condition);
            condition.DurationRounds.Should().Be(1);
            
            // Act - Next Turn (Turn 2) -> Tick -> Duration 0 -> Removed
            turnManager.NextTurn();
            
            // Assert
            creature.Conditions.ActiveConditions.Should().BeEmpty();
        }
    }
}
