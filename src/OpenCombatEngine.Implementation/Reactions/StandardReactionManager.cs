using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Reactions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Events;

namespace OpenCombatEngine.Implementation.Reactions
{
    public class StandardReactionManager : IReactionManager
    {
        private readonly List<IReaction> _reactions = new();
        private readonly ICreature _owner;
        private readonly IGridManager? _grid;

        public IEnumerable<IReaction> AvailableReactions => _reactions.AsReadOnly();

        public StandardReactionManager(ICreature owner, IGridManager? grid = null)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _grid = grid;

            // Subscribe to grid events if available
            if (_grid != null)
            {
                _grid.CreatureMoved += OnCreatureMoved;
            }
        }

        private void OnCreatureMoved(object? sender, MovedEventArgs e)
        {
            // Create context
            var context = new ReactionContext(_grid);
            CheckReactions(e, context);
        }

        public void AddReaction(IReaction reaction)
        {
            ArgumentNullException.ThrowIfNull(reaction);
            _reactions.Add(reaction);
        }

        public void RemoveReaction(IReaction reaction)
        {
            _reactions.Remove(reaction);
        }

        public void CheckReactions(object eventArgs, IReactionContext context)
        {
            // In a real game, we might need to prompt the user.
            // For now, we'll iterate and take the first valid reaction?
            // Or maybe reaction logic should handle priority?
            // "Opportunity Attack" usually asks the player.
            // For automation/bot, we might just take it.
            
            // We assume a simple loop: Try each reaction. If one executes successfully, stop?
            // A creature explicitly has ONE reaction per round usually.
            // Check Action Economy.
            
            if (!_owner.ActionEconomy.HasReaction) return;

            foreach (var reaction in _reactions)
            {
                if (reaction.CanReact(eventArgs, context))
                {
                    // Execute
                    var result = reaction.React(eventArgs, context);
                    if (result.IsSuccess)
                    {
                        // Reaction taken.
                        // ActionEconomy usage should be inside the reaction execution or handled here?
                        // Usually the reaction itself consumes the resource.
                        // But we can verify it was consumed.
                        // Break after one reaction? Yes, usually only one reaction per trigger/turn.
                        break;
                    }
                }
            }
        }
    }

    public class ReactionContext : IReactionContext
    {
        public IGridManager? Grid { get; }

        public ReactionContext(IGridManager? grid)
        {
            Grid = grid;
        }
    }
}
