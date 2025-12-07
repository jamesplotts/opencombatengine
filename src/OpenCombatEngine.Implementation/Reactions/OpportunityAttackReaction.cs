using System;
using OpenCombatEngine.Core.Interfaces.Reactions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Core.Models.Actions;
using System.Linq;
using OpenCombatEngine.Implementation.Actions;

namespace OpenCombatEngine.Implementation.Reactions
{
    public class OpportunityAttackReaction : IReaction
    {
        private readonly ICreature _attacker;

        public string Name => "Opportunity Attack";
        public string Description => "You can make an opportunity attack when a hostile creature that you can see moves out of your reach.";

        public OpportunityAttackReaction(ICreature attacker)
        {
            _attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
        }

        public bool CanReact(object eventArgs, IReactionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (eventArgs is not MovedEventArgs movedArgs) return false;
            
            // Check Reaction availability
            if (!_attacker.ActionEconomy.HasReaction) return false;

            var target = movedArgs.Creature;
            if (target == null || target == _attacker) return false;

            // Check Hostility (Team)
            if (target.Team == _attacker.Team) return false;

            // Check Reach
            var grid = context.Grid;
            if (grid == null) return false;

            var attackerPos = grid.GetPosition(_attacker);
            if (attackerPos == null) return false;

            int reach = grid.GetReach(_attacker);
            int distBefore = grid.GetDistance(attackerPos.Value, movedArgs.From);
            int distAfter = grid.GetDistance(attackerPos.Value, movedArgs.To);

            // Trigger if:
            // 1. Target was within reach
            // 2. Target is now OUT of reach (or leaving the square effectively)
            // 5e Rules: "Actions/Opportunity Attack: You can make an opportunity attack when a hostile creature that you can see moves out of your reach."
            
            bool wasInReach = distBefore <= reach;
            bool leavesReach = distAfter > reach;

            return wasInReach && leavesReach;
        }

        public Result<ActionResult> React(object eventArgs, IReactionContext context)
        {
             ArgumentNullException.ThrowIfNull(context);
             if (eventArgs is not MovedEventArgs movedArgs) return Result<ActionResult>.Failure("Invalid event args.");
             var target = movedArgs.Creature;
             if (target == null) return Result<ActionResult>.Failure("No target.");

             // 1. Consume Reaction
            _attacker.ActionEconomy.UseReaction();

            // 2. Perform Attack
            // Find a melee attack (Unarmed Strike or Main Hand)
            // We look for "Unarmed Strike" explicitly for now, or the first 'AttackAction'.
            
            var attackAction = _attacker.Actions.FirstOrDefault(a => a.Name == "Unarmed Strike" || a.GetType().Name.Contains("Attack", StringComparison.OrdinalIgnoreCase));
            
            if (attackAction != null)
            {
                var actionContext = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                    _attacker,
                    new CreatureTarget(target),
                    context.Grid
                );

                // Note: We ignore ActionEconomy check for the action itself (Standard Action), 
                // because the Reaction allows us to use it.
                // Assuming PerformAction doesn't enforce economy validation (it typically doesn't, it just runs it).
                
                var result = _attacker.PerformAction(attackAction, actionContext);
                return result;
            }
            
            return Result<ActionResult>.Success(new ActionResult(true, $"{_attacker.Name} used reaction but had no attack!"));
        }
    }
}
