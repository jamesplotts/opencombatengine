using System;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Core.Models.Spatial.Shapes
{
    public class CubeShape : IShape
    {
        public int Size { get; } // Length of a side

        public CubeShape(int size)
        {
            Size = size;
        }

        public bool Contains(Position origin, Position point, Position? direction = null)
        {
            // D&D 5e Cube: Origin is a point on a face.
            // For simplicity in this grid system, let's assume the origin is the CENTER of the cube 
            // if we want it to be symmetric, OR the bottom-left-back corner if we want it directional.
            // Without a direction vector, "point on a face" is ambiguous.
            // Let's assume Origin is the CENTER for now to make it easy to place.
            // Range is [-Size/2, +Size/2] from origin.
            
            int halfSize = Size / 2;
            
            int dx = Math.Abs(origin.X - point.X) * 5;
            int dy = Math.Abs(origin.Y - point.Y) * 5;
            int dz = Math.Abs(origin.Z - point.Z) * 5;

            return dx <= halfSize && dy <= halfSize && dz <= halfSize;
        }
    }
}
