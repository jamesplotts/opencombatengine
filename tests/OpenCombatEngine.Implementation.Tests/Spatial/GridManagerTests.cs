using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Spatial;
using Xunit;
using System;
using System.Linq;

namespace OpenCombatEngine.Implementation.Tests.Spatial
{
    public class GridManagerTests
    {
        [Fact]
        public void PlaceCreature_Should_Succeed_If_Empty()
        {
            var grid = new StandardGridManager();
            var creature = Substitute.For<ICreature>();
            creature.Id.Returns(Guid.NewGuid());

            var result = grid.PlaceCreature(creature, new Position(0, 0));

            result.IsSuccess.Should().BeTrue();
            grid.GetPosition(creature).Should().Be(new Position(0, 0));
        }

        [Fact]
        public void PlaceCreature_Should_Fail_If_Occupied()
        {
            var grid = new StandardGridManager();
            var c1 = Substitute.For<ICreature>();
            c1.Id.Returns(Guid.NewGuid());
            var c2 = Substitute.For<ICreature>();
            c2.Id.Returns(Guid.NewGuid());

            grid.PlaceCreature(c1, new Position(0, 0));
            var result = grid.PlaceCreature(c2, new Position(0, 0));

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void MoveCreature_Should_Update_Position()
        {
            var grid = new StandardGridManager();
            var creature = Substitute.For<ICreature>();
            creature.Id.Returns(Guid.NewGuid());

            grid.PlaceCreature(creature, new Position(0, 0));
            var result = grid.MoveCreature(creature, new Position(1, 1));

            result.IsSuccess.Should().BeTrue();
            grid.GetPosition(creature).Should().Be(new Position(1, 1));
            grid.GetCreatureAt(new Position(0, 0)).Should().BeNull();
            grid.GetCreatureAt(new Position(1, 1)).Should().Be(creature);
        }

        [Fact]
        public void GetDistance_Should_Use_Chebyshev_Metric()
        {
            var grid = new StandardGridManager();
            
            // Orthogonal 1 square = 5ft
            grid.GetDistance(new Position(0, 0), new Position(1, 0)).Should().Be(5);
            
            // Diagonal 1 square = 5ft
            grid.GetDistance(new Position(0, 0), new Position(1, 1)).Should().Be(5);
            
            // 2 squares away
            grid.GetDistance(new Position(0, 0), new Position(2, 2)).Should().Be(10);
            
            // 3D Diagonal
            grid.GetDistance(new Position(0, 0, 0), new Position(1, 1, 1)).Should().Be(5);
        }

        [Fact]
        public void GetCreaturesWithin_Should_Return_Correct_Creatures()
        {
            var grid = new StandardGridManager();
            var c1 = Substitute.For<ICreature>();
            c1.Id.Returns(Guid.NewGuid());
            var c2 = Substitute.For<ICreature>();
            c2.Id.Returns(Guid.NewGuid());
            var c3 = Substitute.For<ICreature>();
            c3.Id.Returns(Guid.NewGuid());

            grid.PlaceCreature(c1, new Position(0, 0)); // Center
            grid.PlaceCreature(c2, new Position(2, 0)); // 10ft away
            grid.PlaceCreature(c3, new Position(5, 0)); // 25ft away

            var nearby = grid.GetCreaturesWithin(new Position(0, 0), 15); // Radius 15

            nearby.Should().Contain(c1);
            nearby.Should().Contain(c2);
            nearby.Should().NotContain(c3);
        }
        [Fact]
        public void HasLineOfSight_Should_Return_True_If_Clear()
        {
            var grid = new StandardGridManager();
            // No obstacles
            grid.HasLineOfSight(new Position(0, 0), new Position(5, 0)).Should().BeTrue();
        }

        [Fact]
        public void HasLineOfSight_Should_Return_False_If_Blocked()
        {
            var grid = new StandardGridManager();
            grid.AddObstacle(new Position(2, 0)); // Obstacle in the middle

            grid.HasLineOfSight(new Position(0, 0), new Position(5, 0)).Should().BeFalse();
        }

        [Fact]
        public void HasLineOfSight_Should_Return_True_If_Obstacle_Not_On_Line()
        {
            var grid = new StandardGridManager();
            grid.AddObstacle(new Position(2, 1)); // Obstacle to the side

            grid.HasLineOfSight(new Position(0, 0), new Position(5, 0)).Should().BeTrue();
        }

        [Fact]
        public void HasLineOfSight_Should_Handle_Diagonals()
        {
            var grid = new StandardGridManager();
            // Line from (0,0) to (3,3) passes through (1,1) and (2,2)
            
            grid.AddObstacle(new Position(1, 1));
            grid.HasLineOfSight(new Position(0, 0), new Position(3, 3)).Should().BeFalse();

            grid.RemoveObstacle(new Position(1, 1));
            grid.HasLineOfSight(new Position(0, 0), new Position(3, 3)).Should().BeTrue();
        }
    }
}
