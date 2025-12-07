using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Core.Interfaces.Spatial
{
    /// <summary>
    /// Defines a geometric shape used for Area of Effect targeting.
    /// </summary>
    public interface IShape
    {
        /// <summary>
        /// Determines if a point is within the shape.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <param name="origin">The origin of the effect (e.g. caster position or explosion center).</param>
        /// <param name="target">The target point creating the orientation/direction of the shape (if applicable).</param>
        /// <returns>True if the point is inside the shape.</returns>
        bool Contains(Position point, Position origin, Position target);
    }
}
