using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Core.Models.Actions
{
    public class PositionTarget : IActionTarget
    {
        public Position Position { get; }
        public string Description => $"Position: {Position}";

        public PositionTarget(Position position)
        {
            Position = position;
        }
    }
}
