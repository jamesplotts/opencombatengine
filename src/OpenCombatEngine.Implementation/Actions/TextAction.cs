using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Actions
{
    public class TextAction : IAction
    {
        public string Name { get; }
        public string Description { get; }
        public ActionType Type { get; }

        public TextAction(string name, string description, ActionType type = ActionType.Action)
        {
            Name = name;
            Description = description;
            Type = type;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            // Text actions just return their description as the result message
            return Result<ActionResult>.Success(new ActionResult(true, Description));
        }
    }
}
