using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Core.Interfaces.Spatial
{
    public interface IShape
    {
        bool Contains(Position origin, Position point, Position? direction = null);
    }
}
