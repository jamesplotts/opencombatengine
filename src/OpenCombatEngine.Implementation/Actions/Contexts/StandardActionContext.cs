using System;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;

namespace OpenCombatEngine.Implementation.Actions.Contexts
{
    public class StandardActionContext : IActionContext
    {
        public ICreature Source { get; }
        public IActionTarget Target { get; }
        public IGridManager? Grid { get; }

        public StandardActionContext(ICreature source, IActionTarget target, IGridManager? grid = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Grid = grid;
        }
    }
}
