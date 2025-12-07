using System;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Implementation.Spatial.Shapes
{
    public class ConeShape : IShape
    {
        public int Length { get; }
        // 5e cones width = length.
        // We can make generic angle later if needed.

        public ConeShape(int length)
        {
            Length = length;
        }

        public bool Contains(Position point, Position origin, Position target)
        {
            // If origin == target, no direction. Only include origin.
            if (origin == target) return point == origin;

            // 1. Distance Check
            int dx = point.X - origin.X;
            int dy = point.Y - origin.Y;
            // Ignoring Z logic for simplicity, or project to 2D?
            // Let's do 2D distance for "Cone on ground".
            // Distance squared
            double distSq = dx * dx + dy * dy;
            if (distSq > Length * Length) return false;
            
            // 2. Angle Check
            // Verify point is roughly in front.
            // Vector to Point
            double pLen = Math.Sqrt(distSq);
            if (pLen == 0) return true; // Origin included
            
            double px = dx / pLen;
            double py = dy / pLen;

            // Vector Direction (Origin -> Target)
            int tdx = target.X - origin.X;
            int tdy = target.Y - origin.Y;
            double tLen = Math.Sqrt(tdx * tdx + tdy * tdy);
            double tx = tdx / tLen;
            double ty = tdy / tLen;
            
            double dot = px * tx + py * ty;
            
            // Threshold for ~53 degrees (width = length)
            // Cos(26.5) = 0.8944
            // Let's use 0.89.
            // Note: On a grid, 45 degrees (diagonal) might be desired "Cone".
            // A 90 degree cone (quarter circle) is simpler for grids.
            // 5e rules for grid spell effects usually map cones to:
            // "The width of the cone at a given point along its length is equal to that point's distance from the point of origin."
            // This IS width = length => 53 degrees.
            // However, playing on a square grid, this is awkward.
            // Many tables play 90 degree cones (e.g. Diagonals/Cardinals).
            // A 90 degree cone (Cos(45) = 0.707) covers diagonals nicely.
            // Let's stick to 53 degrees (0.894) for "Rules As Written" geometry, 
            // BUT be aware on a grid this might result in thin cones.
            // Actually, let's use 90 degrees for GRID playability unless user asks for strict Euclidean.
            // Standard 5e "Template" matching usually covers 90 degrees roughly.
            // I'll use 0.707 (45 degrees half-angle -> 90 degrees full).
            // This feels better for a tactical game.
            
            return dot >= 0.707;
        }
    }
}
