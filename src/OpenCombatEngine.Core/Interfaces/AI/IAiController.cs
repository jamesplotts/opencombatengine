using System.Threading.Tasks;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Actions;

namespace OpenCombatEngine.Core.Interfaces.AI
{
    /// <summary>
    /// Represents the "Brain" of a creature, capable of making decisions in combat.
    /// </summary>
    public interface IAiController
    {
        /// <summary>
        /// Determines the next action for the controlled creature.
        /// </summary>
        /// <param name="creature">The creature being controlled.</param>
        /// <param name="context">Context for validating actions (grid, targets).</param>
        /// <returns>The decision containing action and target, or null if no action taken.</returns>
        Task<OpenCombatEngine.Core.Models.AI.AiDecision?> DetermineAction(ICreature creature, IActionContext context);
    }
}
