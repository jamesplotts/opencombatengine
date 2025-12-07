using System;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Implementation.Spatial.Shapes
{
    public class LineShape : IShape
    {
        public int Length { get; }
        public int Width { get; }

        public LineShape(int length, int width = 1)
        {
            Length = length;
            Width = width;
        }

        public bool Contains(Position point, Position origin, Position target)
        {
            // Line from Origin towards Target.
            // If Origin == Target, no direction.
            if (origin == target) return point == origin;

            // Vector origin -> target
            double dirX = target.X - origin.X;
            double dirY = target.Y - origin.Y; // Ignoring Z for 2D line or 3D? Let's stick to 2D for simplicity in logic or generic 3D.
            
            // Normalize
            double len = Math.Sqrt(dirX * dirX + dirY * dirY);
            if (len == 0) return point == origin; // Should not happen given origin != target check above
            
            dirX /= len;
            dirY /= len;
            
            // Vector origin -> point
            double pX = point.X - origin.X;
            double pY = point.Y - origin.Y;
            
            // Project p onto dir
            // dot product
            double dot = pX * dirX + pY * dirY;
            
            // Check if projection is within [0, Length]
            if (dot < 0 || dot > Length) return false;
            
            // Check perpendicular distance
            // Dist sq = |P|^2 - dot^2
            double pLenSq = pX * pX + pY * pY;
            double distSq = pLenSq - (dot * dot);
            
            // Width is "Diameter", usually. So radius = Width / 2.
            // BUT width 1 means 0.5 radius.
            // 5e Lines are usually 5ft wide (1 square).
            // We want to hit the squares intersecting the line.
            // Standard "thick line" rasterization.
            // Or simple distance check: dist <= Width / 2.
            // If Width = 1, radius = 0.5.
            // Center of tile is (X, Y).
            // Line passes through coordinate space.
            // Distance 0.5 means any overlap?
            // Let's use 0.5 threshold which effectively catches tiles the line passes through center of.
            // Or slightly generous 0.707?
            // "If the line passes through the square".
            // For now, dist <= Width / 2.0.
            
            return distSq <= ((Width / 2.0) * (Width / 2.0));
        }
    }
}
