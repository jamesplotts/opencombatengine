using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    /// <summary>
    /// Represents an active ability on a magic item.
    /// </summary>
    public interface IMagicItemAbility
    {
        /// <summary>
        /// Gets the name of the ability.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the ability.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the charge cost to use this ability.
        /// </summary>
        int Cost { get; }

        /// <summary>
        /// Gets the type of action required to use this ability.
        /// </summary>
        ActionType ActionType { get; }

        /// <summary>
        /// Executes the ability.
        /// </summary>
        /// <param name="user">The creature using the item.</param>
        /// <param name="context">The context of the action (targets, etc.).</param>
        /// <returns>Result of the execution.</returns>
        Result<bool> Execute(ICreature user, IActionContext context);
    }
}
