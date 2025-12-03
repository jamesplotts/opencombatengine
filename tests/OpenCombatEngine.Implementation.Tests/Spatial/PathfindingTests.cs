using FluentAssertions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Spatial;
using System.Linq;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spatial
{
    public class PathfindingTests
    {
        private readonly StandardGridManager _gridManager;

        public PathfindingTests()
        {
            _gridManager = new StandardGridManager();
        }

        [Fact]
        public void Should_Find_Direct_Path_In_Open_Space()
        {
            var start = new Position(0, 0, 0);
            var end = new Position(0, 2, 0);

            var path = _gridManager.GetPath(start, end).ToList();

            path.Should().HaveCount(3); // 0,0 -> 0,1 -> 0,2
            path.Last().Should().Be(end);
        }

        [Fact]
        public void Should_Avoid_Obstacles()
        {
            // S . E
            // . X .
            // . . .
            // Wall at (0,1,0) blocking direct path from (0,0,0) to (0,2,0)?
            // No, (0,1,0) is between.
            
            var start = new Position(0, 0, 0);
            var end = new Position(0, 2, 0);
            
            _gridManager.AddObstacle(new Position(0, 1, 0));

            var path = _gridManager.GetPath(start, end).ToList();

            path.Should().NotBeEmpty();
            path.Should().NotContain(new Position(0, 1, 0));
            path.Last().Should().Be(end);
            
            // Should go around, e.g. (1,0) -> (1,1) -> (1,2) -> (0,2) or similar
            // Or diagonal: (1,1) -> (0,2)
        }

        [Fact]
        public void Should_Respect_Difficult_Terrain_Cost()
        {
            // Start (0,0), End (0,3)
            // (0,1) is Difficult (10 cost)
            // (1,1) is Normal (5 cost)
            // Path should prefer going around if cheaper.
            // Direct: 0->1(Diff)->2->3 = 10+5+5 = 20
            // Around: 0->(1,1)->(1,2)->(0,3) = 5+5+5 = 15?
            // Wait, diagonals cost 5.
            // 0,0 -> 1,1 (5) -> 0,2 (5) -> 0,3 (5) = 15.
            // 0,0 -> 0,1 (10) -> 0,2 (5) -> 0,3 (5) = 20.
            
            var start = new Position(0, 0, 0);
            var end = new Position(0, 3, 0);
            
            _gridManager.AddDifficultTerrain(new Position(0, 1, 0));
            
            var path = _gridManager.GetPath(start, end).ToList();
            var cost = _gridManager.GetPathCost(start, end);
            
            cost.Should().Be(15);
            path.Should().NotContain(new Position(0, 1, 0));
        }

        [Fact]
        public void Should_Return_Empty_If_Unreachable()
        {
            var start = new Position(0, 0, 0);
            var end = new Position(0, 2, 0);
            
            // Surround end with obstacles
            _gridManager.AddObstacle(new Position(0, 1, 0));
            _gridManager.AddObstacle(new Position(1, 1, 0));
            _gridManager.AddObstacle(new Position(1, 2, 0));
            _gridManager.AddObstacle(new Position(1, 3, 0));
            _gridManager.AddObstacle(new Position(0, 3, 0));
            _gridManager.AddObstacle(new Position(-1, 3, 0));
            _gridManager.AddObstacle(new Position(-1, 2, 0));
            _gridManager.AddObstacle(new Position(-1, 1, 0));
            // Also diagonals? A* can move diagonally.
            // Need to block all 8 neighbors of (0,2).
            
            // Let's just block the start completely.
            _gridManager.AddObstacle(new Position(1, 0, 0));
            _gridManager.AddObstacle(new Position(1, 1, 0));
            _gridManager.AddObstacle(new Position(0, 1, 0));
            _gridManager.AddObstacle(new Position(-1, 1, 0));
            _gridManager.AddObstacle(new Position(-1, 0, 0));
            _gridManager.AddObstacle(new Position(-1, -1, 0));
            _gridManager.AddObstacle(new Position(0, -1, 0));
            _gridManager.AddObstacle(new Position(1, -1, 0));
            
            // Block Z-axis escape
            _gridManager.AddObstacle(new Position(0, 0, 1));
            _gridManager.AddObstacle(new Position(0, 0, -1));
            // And diagonals in Z? Yes, A* is fully 3D.
            // To truly isolate (0,0,0), we need to block all 26 neighbors.
            // Easier: Just block the destination (0,2,0) completely.
            // Or just assert path is found (since it's hard to block everything in 3D without a loop).
            // Let's use a loop to block all neighbors of start.
            
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && y == 0 && z == 0) continue;
                _gridManager.AddObstacle(new Position(x, y, z));
            }
            
            var path = _gridManager.GetPath(start, end).ToList();
            path.Should().BeEmpty();
        }
    }
}
