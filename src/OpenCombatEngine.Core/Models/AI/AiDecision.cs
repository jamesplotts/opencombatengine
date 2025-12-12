using OpenCombatEngine.Core.Interfaces.Actions;

namespace OpenCombatEngine.Core.Models.AI
{
    public class AiDecision
    {
        public IAction Action { get; }
        public IActionTarget Target { get; }

        public AiDecision(IAction action, IActionTarget target)
        {
            Action = action ?? throw new System.ArgumentNullException(nameof(action));
            Target = target ?? throw new System.ArgumentNullException(nameof(target));
        }
    }
}
