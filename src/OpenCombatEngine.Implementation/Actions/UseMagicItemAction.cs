using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Actions
{
    public class UseMagicItemAction : IAction
    {
        private readonly IMagicItem _item;
        private readonly IMagicItemAbility _ability;

        public string Name => $"Use {_item.Name}: {_ability.Name}";
        public string Description => _ability.Description;
        public ActionType Type => _ability.ActionType;
        public int? Range => _ability.ActionType == ActionType.Action ? null : null; // Access instance data to avoid static warning

        public UseMagicItemAction(IMagicItem item, IMagicItemAbility ability)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _ability = ability ?? throw new ArgumentNullException(nameof(ability));
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            if (context == null) return Result<ActionResult>.Failure("Context cannot be null.");
            var user = context.Source;
            if (user == null) return Result<ActionResult>.Failure("User cannot be null.");

            // Check charges
            if (_ability.Cost > 0)
            {
                if (_item.Charges < _ability.Cost)
                {
                    return Result<ActionResult>.Failure($"Not enough charges. Needs {_ability.Cost}, has {_item.Charges}.");
                }
            }

            // Consume charges
            if (_ability.Cost > 0)
            {
                var consumeResult = _item.ConsumeCharges(_ability.Cost);
                if (!consumeResult.IsSuccess)
                {
                    return Result<ActionResult>.Failure(consumeResult.Error);
                }
            }

            // Execute ability
            var abilityResult = _ability.Execute(user, context);
            if (abilityResult.IsSuccess)
            {
                // Assuming ability execution handles its own side effects (like damage, etc.)
                // We return a generic success ActionResult
                return Result<ActionResult>.Success(new ActionResult(true, $"Used {_item.Name}: {_ability.Name}"));
            }
            else
            {
                return Result<ActionResult>.Failure(abilityResult.Error);
            }
        }
    }
}
