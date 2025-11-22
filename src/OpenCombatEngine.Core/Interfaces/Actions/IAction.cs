using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Actions
{
    /// <summary>
    /// Defines an action that a creature can perform.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Gets the name of the action.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the action.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the type of action (Action, BonusAction, etc.).
        /// </summary>
        ActionType Type { get; }

        /// <summary>
        /// Executes the action against a target.
        /// </summary>
        /// <param name="source">The creature performing the action.</param>
        /// <param name="target">The target of the action.</param>
        /// <returns>The result of the action.</returns>
        Result<ActionResult> Execute(ICreature source, ICreature target);
    }
}
