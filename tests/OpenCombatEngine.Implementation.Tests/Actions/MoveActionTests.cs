using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Creatures;
using System;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    public class MoveActionTests
    {
        [Fact]
        public void Execute_Should_Move_Creature_When_Movement_Available()
        {
            // Arrange
            var movement = Substitute.For<IMovement>();
            movement.MovementRemaining.Returns(30);
            
            var creature = Substitute.For<ICreature>();
            creature.Movement.Returns(movement);

            var action = new MoveAction(15);

            // Act
            var result = action.Execute(creature, null!);

            // Assert
            result.IsSuccess.Should().BeTrue();
            movement.Received(1).Move(15);
        }

        [Fact]
        public void Execute_Should_Fail_When_Not_Enough_Movement()
        {
            // Arrange
            var movement = Substitute.For<IMovement>();
            movement.MovementRemaining.Returns(10);
            
            var creature = Substitute.For<ICreature>();
            creature.Movement.Returns(movement);

            var action = new MoveAction(15);

            // Act
            var result = action.Execute(creature, null!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Not enough movement");
            movement.DidNotReceive().Move(Arg.Any<int>());
        }

        [Fact]
        public void Execute_Should_Fail_When_Creature_Has_No_Movement()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            creature.Movement.Returns((IMovement)null);

            var action = new MoveAction(15);

            // Act
            var result = action.Execute(creature, null!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("no movement capability");
        }

        [Fact]
        public void Constructor_Should_Throw_On_Invalid_Distance()
        {
            // Act
            Action act = () => new MoveAction(0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
