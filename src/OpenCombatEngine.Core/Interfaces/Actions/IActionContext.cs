using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;

namespace OpenCombatEngine.Core.Interfaces.Actions
{
    public interface IActionContext
    {
        ICreature Source { get; }
        IActionTarget Target { get; }
        IGridManager? Grid { get; }
    }
}
