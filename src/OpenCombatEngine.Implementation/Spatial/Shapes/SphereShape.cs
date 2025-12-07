using System;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Implementation.Spatial.Shapes
{
    public class SphereShape : IShape
    {
        public int Radius { get; }

        public SphereShape(int radius)
        {
            Radius = radius;
        }

        public bool Contains(Position point, Position origin, Position target)
        {
            // Simple Euclidean distance squared check
            // assuming grid units.
            // (x - ox)^2 + (y - oy)^2 <= r^2
            
            int dx = point.X - origin.X;
            int dy = point.Y - origin.Y;
            int dz = point.Z - origin.Z; // Include Z for completeness, though often 2D

            double distSq = dx * dx + dy * dy + dz * dz;
            return distSq <= (Radius * Radius);
        }
    }
}
