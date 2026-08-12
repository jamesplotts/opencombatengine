using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;

namespace OpenCombatEngine.Core.Interfaces.Actions
{
    public interface IActionContext
    {
        ICreature Source { get; }
        IActionTarget Target { get; }
        IGridManager? Grid { get; }
        OpenCombatEngine.Core.Enums.CoverType TargetCover { get; }
        OpenCombatEngine.Core.Enums.ObscurementType TargetObscurement { get; }

        /// <summary>
        /// When true, the action being executed should not gate on or consume the source's
        /// Action/BonusAction/Reaction economy. Used when a caller (e.g. a reaction handler)
        /// has already accounted for the correct resource itself.
        /// </summary>
        bool BypassActionEconomy { get; }
    }
}
