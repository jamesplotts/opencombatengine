using OpenCombatEngine.Core.Results;
using System;

namespace OpenCombatEngine.Core.Interfaces.Reactions
{
    public interface IReactionContext
    {
        // Context for the reaction, e.g. the Grid, the Source, etc.
        // For now, it might be empty or provide access to game state.
        OpenCombatEngine.Core.Interfaces.Spatial.IGridManager? Grid { get; }
    }

    public interface IReaction
    {
        string Name { get; }
        string Description { get; }

        /// <summary>
        /// Checks if the reaction can be triggered by the given event.
        /// </summary>
        /// <param name="eventArgs">The event arguments (e.g. MovedEventArgs).</param>
        /// <param name="context">The context.</param>
        /// <returns>True if the reaction can be taken.</returns>
        bool CanReact(object eventArgs, IReactionContext context);

        /// <summary>
        /// Executes the reaction.
        /// </summary>
        /// <param name="eventArgs">The event arguments.</param>
        /// <param name="context">The context.</param>
        /// <returns>Result of the reaction.</returns>
        Result<OpenCombatEngine.Core.Models.Actions.ActionResult> React(object eventArgs, IReactionContext context);
    }
}
