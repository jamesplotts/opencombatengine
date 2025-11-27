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
        public OpenCombatEngine.Core.Enums.CoverType TargetCover { get; }
        public OpenCombatEngine.Core.Enums.ObscurementType TargetObscurement { get; }

        public StandardActionContext(
            ICreature source, 
            IActionTarget target, 
            IGridManager? grid = null,
            OpenCombatEngine.Core.Enums.CoverType targetCover = OpenCombatEngine.Core.Enums.CoverType.None,
            OpenCombatEngine.Core.Enums.ObscurementType targetObscurement = OpenCombatEngine.Core.Enums.ObscurementType.None)
        {
            Source = source ?? throw new System.ArgumentNullException(nameof(source));
            Target = target ?? throw new System.ArgumentNullException(nameof(target));
            Grid = grid;
            TargetCover = targetCover;
            TargetObscurement = targetObscurement;
        }
    }
}
