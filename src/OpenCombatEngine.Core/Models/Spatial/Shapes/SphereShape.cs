using System;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Core.Models.Spatial.Shapes
{
    public class SphereShape : IShape
    {
        public int Radius { get; }

        public SphereShape(int radius)
        {
            Radius = radius;
        }

        public bool Contains(Position origin, Position point, Position? direction = null)
        {
            // Simple distance check using Chebyshev distance (consistent with GridManager)
            // Or Euclidean? D&D 5e usually uses "squares" which is Chebyshev-like (5-5-5 diagonals) 
            // or "every other diagonal counts as 10" (Euclidean-ish approximation).
            // Our GridManager uses Chebyshev (Max(dx, dy, dz) * 5).
            // Let's stick to that for consistency.
            
            int dx = Math.Abs(origin.X - point.X);
            int dy = Math.Abs(origin.Y - point.Y);
            int dz = Math.Abs(origin.Z - point.Z);

            int distance = Math.Max(dx, Math.Max(dy, dz)) * 5;
            
            return distance <= Radius;
        }
    }
}
