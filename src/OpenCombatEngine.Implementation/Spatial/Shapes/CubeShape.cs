using System;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Implementation.Spatial.Shapes
{
    public class CubeShape : IShape
    {
        public int Size { get; }

        public CubeShape(int size)
        {
            Size = size;
        }

        public bool Contains(Position point, Position origin, Position target)
        {
            // Axis-aligned cube centered on origin
            // If Size is 3 (3x3), radius is 1.5? Or extends -1 to +1?
            // "Size" usually means edge length.
            // Half-size logic.
            // If Size is odd (1, 3, 5), it centers perfectly on a tile.
            // If Size is even (2, 4), it centers on a vertex.
            // Our Position is integer tile coordinates.
            // Let's assume origin covers the center tile.
            // Radius = (Size - 1) / 2
            
            // Example: Size 3 (3x3 grid). Origin is center.
            // Range: [Origin - 1, Origin + 1]
            // Radius = 1.
            
            // Example: Size 1. Radius = 0.
            
            // Formula: |dx| <= radius && |dy| <= radius
            
            // If Size is even (e.g. 2), we can't center on an integer tile perfectly symmetrically without bias.
            // We'll use floor((Size) / 2) as extent?
            // No, Size 2 should be 2x2. Origin top-left?
            // Let's assume standard "Radius" extension for cubes in grid often means "Chebyshev radius".
            // So a "15ft Cube" (3 squares) -> Chebyshev distance <= 1 from center.
            // Size 3 => dist <= 1.
            // Formula: dist <= (Size / 2). (Integer division).
            // 3 / 2 = 1. Correct.
            // 1 / 2 = 0. Correct.
            // 4 / 2 = 2. -> 5x5?
            // A "20ft Cube" is 4x4.
            // If centered on tile, a radius of 2 gives 5x5.
            // Implication: Odd sizes work best for tile-centered logic.
            // For now, I will implement Chebyshev radius based on size.
            // If precise 2x2 is needed, we need "Origin is vertex" support which requires sub-grid coords or offset logic.
            // I'll stick to simplified tile-centered logic: Extent = Size / 2.
            
            int radius = Size / 2;
            int dx = Math.Abs(point.X - origin.X);
            int dy = Math.Abs(point.Y - origin.Y);
            int dz = Math.Abs(point.Z - origin.Z);
            
            return dx <= radius && dy <= radius && dz <= radius;
        }
    }
}
