using System.Threading.Tasks;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Actions;

namespace OpenCombatEngine.Core.Interfaces.AI
{
    /// <summary>
    /// Orchestrates the execution of turns for AI-controlled participants.
    /// </summary>
    public interface ICombatRunner
    {
        /// <summary>
        /// Executes the turn for the current creature if it has an AI controller.
        /// </summary>
        /// <param name="turnManager">The active turn manager.</param>
        /// <param name="context">The combat context for action validation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RunTurn(ITurnManager turnManager, IActionContext context);
    }
}
