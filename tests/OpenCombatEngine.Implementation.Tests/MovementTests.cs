using System;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class MovementTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties()
        {
            // Arrange
            var stats = new StandardCombatStats(speed: 40);
            var conditions = Substitute.For<IConditionManager>();
            var movement = new StandardMovement(stats, conditions);

            // Assert
            movement.Speed.Should().Be(40);
            movement.MovementRemaining.Should().Be(40);
        }

        [Fact]
        public void Move_Should_Throw_When_Grappled()
        {
            // Arrange
            var stats = new StandardCombatStats(speed: 30);
            var conditions = Substitute.For<IConditionManager>();
            conditions.HasCondition(ConditionType.Grappled).Returns(true);
            var movement = new StandardMovement(stats, conditions);

            // Act
            Action act = () => movement.Move(5);

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*Grappled*");
        }

        [Fact]
        public void ResetTurn_Should_Set_Movement_To_Zero_When_Grappled()
        {
            // Arrange
            var stats = new StandardCombatStats(speed: 30);
            var conditions = Substitute.For<IConditionManager>();
            conditions.HasCondition(ConditionType.Grappled).Returns(true);
            var movement = new StandardMovement(stats, conditions);

            // Act
            movement.ResetTurn();

            // Assert
            movement.MovementRemaining.Should().Be(0);
        }

        [Fact]
        public void ResetTurn_Should_Restore_Speed()
        {
            // Arrange
            var stats = new StandardCombatStats(speed: 30);
            var conditions = Substitute.For<IConditionManager>();
            var movement = new StandardMovement(stats, conditions);
            movement.Move(30);

            // Act
            movement.ResetTurn();

            // Assert
            movement.MovementRemaining.Should().Be(30);
        }

        [Fact]
        public void Move_Should_Decrease_Remaining_Movement()
        {
            // Arrange
            var stats = new StandardCombatStats(speed: 30);
            var conditions = Substitute.For<IConditionManager>();
            var movement = new StandardMovement(stats, conditions);

            // Act
            movement.Move(10);

            // Assert
            movement.MovementRemaining.Should().Be(20);
        }

        [Fact]
        public void Move_Should_Throw_If_Not_Enough_Movement()
        {
            // Arrange
            var stats = new StandardCombatStats(speed: 30);
            var conditions = Substitute.For<IConditionManager>();
            var movement = new StandardMovement(stats, conditions);

            // Act
            Action act = () => movement.Move(35);

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }


        
        [Fact]
        public void StandardCreature_StartTurn_Should_Reset_Movement()
        {
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Hero", new StandardAbilityScores(), new StandardHitPoints(10));
            // Default speed is 0 in StandardCombatStats if not set? 
            // Let's check StandardCombatStats default.
            // StandardCombatStats defaults: AC 10, Init 0, Speed 30.
            
            creature.Movement.Move(10);
            creature.Movement.MovementRemaining.Should().Be(20);
            
            creature.StartTurn();
            
            creature.Movement.MovementRemaining.Should().Be(30);
        }
    }
}
