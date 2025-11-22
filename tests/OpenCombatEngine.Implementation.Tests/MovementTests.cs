using System;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class MovementTests
    {
        [Fact]
        public void Should_Initialize_With_Speed_From_Stats()
        {
            var stats = Substitute.For<ICombatStats>();
            stats.Speed.Returns(30);
            
            var movement = new StandardMovement(stats);
            
            movement.Speed.Should().Be(30);
            movement.MovementRemaining.Should().Be(30);
        }

        [Fact]
        public void Move_Should_Reduce_Remaining_Movement()
        {
            var stats = Substitute.For<ICombatStats>();
            stats.Speed.Returns(30);
            var movement = new StandardMovement(stats);

            movement.Move(10);
            
            movement.MovementRemaining.Should().Be(20);
        }

        [Fact]
        public void Move_Should_Throw_If_Not_Enough_Movement()
        {
            var stats = Substitute.For<ICombatStats>();
            stats.Speed.Returns(30);
            var movement = new StandardMovement(stats);

            Action act = () => movement.Move(35);
            
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ResetTurn_Should_Restore_Movement()
        {
            var stats = Substitute.For<ICombatStats>();
            stats.Speed.Returns(30);
            var movement = new StandardMovement(stats);

            movement.Move(30);
            movement.MovementRemaining.Should().Be(0);

            movement.ResetTurn();
            
            movement.MovementRemaining.Should().Be(30);
        }
        
        [Fact]
        public void StandardCreature_StartTurn_Should_Reset_Movement()
        {
            var creature = new StandardCreature("Hero", new StandardAbilityScores(), new StandardHitPoints(10));
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
