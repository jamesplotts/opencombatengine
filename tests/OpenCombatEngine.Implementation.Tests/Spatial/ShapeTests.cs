using FluentAssertions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Models.Spatial.Shapes;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spatial
{
    public class ShapeTests
    {
        [Fact]
        public void SphereShape_Should_Contain_Points_Within_Radius()
        {
            var sphere = new SphereShape(10); // 10ft radius (2 squares)
            var origin = new Position(0, 0, 0);

            // Center
            sphere.Contains(origin, new Position(0, 0, 0)).Should().BeTrue();
            
            // 5ft away
            sphere.Contains(origin, new Position(1, 0, 0)).Should().BeTrue();
            
            // 10ft away
            sphere.Contains(origin, new Position(2, 0, 0)).Should().BeTrue();
            
            // 15ft away (Outside)
            sphere.Contains(origin, new Position(3, 0, 0)).Should().BeFalse();
            
            // Diagonal 10ft (Chebyshev: 2 squares diagonal is 10ft)
            sphere.Contains(origin, new Position(2, 2, 0)).Should().BeTrue();
        }

        [Fact]
        public void CubeShape_Should_Contain_Points_Within_Bounds()
        {
            var cube = new CubeShape(20); // 20ft cube (4 squares side). Half-size = 10ft (2 squares)
            var origin = new Position(0, 0, 0); // Center

            // Center
            cube.Contains(origin, new Position(0, 0, 0)).Should().BeTrue();
            
            // 10ft away (Edge)
            cube.Contains(origin, new Position(2, 0, 0)).Should().BeTrue();
            
            // 15ft away (Outside)
            cube.Contains(origin, new Position(3, 0, 0)).Should().BeFalse();
            
            // Corner (10, 10, 10) -> (2, 2, 2) squares
            cube.Contains(origin, new Position(2, 2, 2)).Should().BeTrue();
        }
    }
}
