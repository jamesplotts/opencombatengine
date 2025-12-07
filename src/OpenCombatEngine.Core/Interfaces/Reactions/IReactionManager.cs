using System;
using System.Collections.Generic;

namespace OpenCombatEngine.Core.Interfaces.Reactions
{
    public interface IReactionManager
    {
        IEnumerable<IReaction> AvailableReactions { get; }
        void AddReaction(IReaction reaction);
        void RemoveReaction(IReaction reaction);
        
        /// <summary>
        /// Called when an event occurs that might trigger reactions.
        /// Usually hooked up to global events or called manually by the creature/turn manager.
        /// </summary>
        void CheckReactions(object eventArgs, IReactionContext context);
    }
}
