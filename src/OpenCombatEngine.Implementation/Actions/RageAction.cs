using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Conditions;

namespace OpenCombatEngine.Implementation.Actions
{
    public class RageAction : IAction
    {
        public string Name => "Rage";
        public string Description => "Enter a rage for 1 minute.";
        public ActionType Type => ActionType.BonusAction;

        public Result<ActionResult> Execute(IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var source = context.Source;
            // Target is irrelevant, self-buff.

            if (source.ActionEconomy != null)
            {
                if (!source.ActionEconomy.HasBonusAction)
                {
                    return Result<ActionResult>.Failure("No bonus action available.");
                }
                source.ActionEconomy.UseBonusAction();
            }

            var rage = new RageCondition();
            var result = source.Conditions.AddCondition(rage);

            if (result.IsSuccess)
            {
                return Result<ActionResult>.Success(new ActionResult(true, "Entered Rage!"));
            }
            else
            {
                return Result<ActionResult>.Failure($"Failed to enter rage: {result.Error}");
            }
        }
    }
}
