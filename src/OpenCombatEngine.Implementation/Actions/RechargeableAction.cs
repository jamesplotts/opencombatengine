using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Core.Models.Actions;

namespace OpenCombatEngine.Implementation.Actions
{
    public class RechargeableAction : IAction
    {
        private readonly IAction _innerAction;
        public int MinRechargeRoll { get; }
        public bool IsAvailable { get; private set; } = true;

        public string Name => $"{_innerAction.Name} (Recharge {MinRechargeRoll}-6)";
        public string Description => _innerAction.Description;
        public ActionType Type => _innerAction.Type;

        public RechargeableAction(IAction innerAction, int minRechargeRoll)
        {
            _innerAction = innerAction ?? throw new ArgumentNullException(nameof(innerAction));
            if (minRechargeRoll < 1 || minRechargeRoll > 6) throw new ArgumentOutOfRangeException(nameof(minRechargeRoll), "Recharge roll must be between 1 and 6.");
            MinRechargeRoll = minRechargeRoll;
        }

        public bool TryRecharge(int roll)
        {
            if (roll >= MinRechargeRoll)
            {
                IsAvailable = true;
                return true;
            }
            return false;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            if (!IsAvailable)
            {
                return Result<ActionResult>.Failure($"{Name} is not recharged.");
            }

            var result = _innerAction.Execute(context);
            if (result.IsSuccess)
            {
                IsAvailable = false;
            }
            return result;
        }
    }
}
