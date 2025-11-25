using FluentAssertions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Spatial;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spatial
{
    public class GridManagerCostTests
    {
        [Fact]
        public void GetPathCost_Should_Return_Standard_Cost_For_Clear_Path()
        {
            var grid = new StandardGridManager();
            var start = new Position(0, 0, 0);
            var end = new Position(2, 0, 0); // 2 squares away

            // 5 + 5 = 10
            grid.GetPathCost(start, end).Should().Be(10);
        }

        [Fact]
        public void GetPathCost_Should_Double_Cost_For_Difficult_Terrain()
        {
            var grid = new StandardGridManager();
            var start = new Position(0, 0, 0);
            var end = new Position(2, 0, 0); // 2 squares away

            // Mark (1,0,0) and (2,0,0) as difficult
            grid.AddDifficultTerrain(new Position(1, 0, 0));
            grid.AddDifficultTerrain(new Position(2, 0, 0));

            // 10 + 10 = 20
            // A* finds path around difficult terrain: 0,0 -> 1,1 -> 0,2 -> 0,3 (Cost 15)
            // Or similar. Direct path is 20.
            grid.GetPathCost(start, end).Should().Be(15);
        }

        [Fact]
        public void GetPathCost_Should_Handle_Mixed_Terrain()
        {
            var grid = new StandardGridManager();
            var start = new Position(0, 0, 0);
            var end = new Position(0, 2, 0);
            
            grid.AddDifficultTerrain(new Position(0, 1, 0));
            
            // Direct: 15. Around: 10.
            grid.GetPathCost(start, end).Should().Be(10);
        }
    }
}
