using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Items
{
    public class MagicItemAbility : IMagicItemAbility
    {
        public string Name { get; }
        public string Description { get; }
        public int Cost { get; }
        public ActionType ActionType { get; }

        private readonly Func<ICreature, IActionContext, Result<bool>> _executionLogic;

        public MagicItemAbility(string name, string description, int cost, ActionType actionType, Func<ICreature, IActionContext, Result<bool>> executionLogic)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
            Description = description;
            Cost = cost < 0 ? 0 : cost;
            ActionType = actionType;
            _executionLogic = executionLogic ?? throw new ArgumentNullException(nameof(executionLogic));
        }

        public Result<bool> Execute(ICreature user, IActionContext context)
        {
            return _executionLogic(user, context);
        }
    }
}
