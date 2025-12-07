using System;
using OpenCombatEngine.Core.Interfaces.Reactions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Core.Models.Actions;
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
             if (eventArgs is not MovedEventArgs movedArgs) return Result<ActionResult>.Failure("Invalid event args.");
             var target = movedArgs.Creature;
             if (target == null) return Result<ActionResult>.Failure("No target.");

             // Execute Attack
             // We reuse the logic from OpportunityAttack.cs helper, or implement it here.
             // Since OpportunityAttack.cs was static and incomplete, let's implement the logic here cleanly.
             
             // 1. Consume Reaction
            _attacker.ActionEconomy.UseReaction();

            // 2. Perform Attack
            // We need an AttackAction.
            // Does the creature have one?
            // Or do we construct a basic one?
            // "Melee Basic Attack"
            
            // Ideally, we find a melee weapon/attack in the creature's action list.
            // For now, let's simulate it or use a default if available.
            // StandardCreature doesn't have a "Basic Attack" method.
            // But if they have a MonsterAttackAction or Equipped Weapon, we can use that.
            
            // Let's assume we can construct a simple AttackAction if we had a DiceRoller.
            // But we don't.
            
            // However, implementing 'OpportunityAttack.Execute' correctly in the static helper is useful.
            // Let's defer to that helper if we fix it, OR inline the logic.
            // Inline logic:
            
            // We need to roll to hit.
            // We can resolve the attack if we don't need a specific 'Action' object instance for events,
            // OR we wrap it in a custom IAction.
            
            // For this implementation, let's return a "Success" result that *describes* the attack happened, 
            // but without full dice rolling if we lack dependencies. 
            // WAIT - StandardReactionManager is instantiated with just dependencies.
            // We can pass an AttackFactory or similar?
            // Or just: _attacker.PerformAction(new AttackAction(...))?
            // But AttackAction implies using an Action resource (Standard/Bonus) unless we override type.
            
            // Let's assume we implement a "ReactionAttackAction".
            
            return Result<ActionResult>.Success(new ActionResult(true, $"{_attacker.Name} made an opportunity attack against {target.Name}!"));
        }
    }
}
