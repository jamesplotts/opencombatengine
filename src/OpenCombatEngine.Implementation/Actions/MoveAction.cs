using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions.Contexts;

namespace OpenCombatEngine.Implementation.Actions
{
    public class MoveAction : IAction
    {
        public string Name => "Move";
        public string Description => $"Move {_distance} feet.";
        public ActionType Type => ActionType.Movement;

        private readonly int _distance;
        private readonly IDiceRoller? _diceRoller;

        public MoveAction(int distance, IDiceRoller? diceRoller = null)
        {
            if (distance <= 0) throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be positive.");
            _distance = distance;
            _diceRoller = diceRoller;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var source = context.Source;

            if (source.Movement == null)
            {
                return Result<ActionResult>.Failure("Creature has no movement capability.");
            }

            // If Grid is present, validate position and distance
            if (context.Grid != null)
            {
                if (context.Target is not PositionTarget positionTarget)
                {
                    return Result<ActionResult>.Failure("Target must be a position for grid movement.");
                }

                var currentPos = context.Grid.GetPosition(source);
                if (currentPos == null)
                {
                    return Result<ActionResult>.Failure("Creature is not on the grid.");
                }

                var targetPos = positionTarget.Position;
                
                // Get Path
                var path = context.Grid.GetPath(currentPos.Value, targetPos).ToList();
                
                // Path includes start. We want steps.
                // If path is just start, we didn't move.
                if (path.Count <= 1)
                {
                     return Result<ActionResult>.Success(new ActionResult(true, "Did not move."));
                }

                int totalCost = 0;
                Position lastPos = currentPos.Value;
                
                // Iterate steps
                // Skip the first one (start)
                for (int i = 1; i < path.Count; i++)
                {
                    var nextPos = path[i];
                    var stepCost = context.Grid.GetPathCost(lastPos, nextPos);
                    
                    // Check limits
                    if (totalCost + stepCost > _distance)
                    {
                        // Can't move further with this action
                        break; 
                    }
                    if (source.Movement.MovementRemaining < stepCost)
                    {
                        // Can't move further this turn
                        break;
                    }

                    // OPPORTUNITY ATTACK CHECK
                    // Before moving from lastPos to nextPos, check if we provoke.
                    // We provoke if we leave a hostile's reach.
                    
                    // 1. Find all hostiles
                    // Optimization: Get creatures within max reach (10ft) of lastPos?
                    // Or just get all creatures and filter.
                    // Grid.GetCreaturesWithin(lastPos, 10) is good.
                    var potentialAttackers = context.Grid.GetCreaturesWithin(lastPos, 15); // 15 to be safe
                    
                    foreach (var attacker in potentialAttackers)
                    {
                        if (attacker.Id == source.Id) continue; // Skip self
                        if (attacker.Team == source.Team) continue; // Skip allies (assuming Team string match)
                        if (!attacker.ActionEconomy.HasReaction) continue; // No reaction
                        
                        // Check if we are currently in reach
                        int reach = context.Grid.GetReach(attacker);
                        
                        var attackerPos = context.Grid.GetPosition(attacker);
                        if (attackerPos == null) continue;
                        
                        int distToCurrentStep = context.Grid.GetDistance(attackerPos.Value, lastPos);
                        
                        if (distToCurrentStep <= reach)
                        {
                            // We are in reach. Are we leaving it?
                            int distToNextStep = context.Grid.GetDistance(attackerPos.Value, nextPos);
                            if (distToNextStep > reach)
                            {
                                // PROVOKED!
                                // Resolve Attack
                                ResolveOpportunityAttack(attacker, source, context.Grid);
                                
                                // Did we die?
                                if (source.HitPoints.Current <= 0)
                                {
                                    // Dead/Unconscious. Stop movement.
                                    // Update grid to lastPos (where we died? or do we fall prone there?)
                                    // Let's assume we stop at lastPos.
                                    // But we haven't updated grid yet.
                                    // So we just return.
                                    return Result<ActionResult>.Success(new ActionResult(true, $"Moved to {lastPos} and was stopped by Opportunity Attack (Unconscious). Cost: {totalCost}."));
                                }
                            }
                        }
                    }

                    // Move successful (so far)
                    var moveResult = context.Grid.MoveCreature(source, nextPos);
                    if (!moveResult.IsSuccess)
                    {
                        return Result<ActionResult>.Failure(moveResult.Error);
                    }
                    
                    source.Movement.Move(stepCost);
                    totalCost += stepCost;
                    lastPos = nextPos;
                }

                return Result<ActionResult>.Success(new ActionResult(true, $"Moved to {lastPos}. Cost: {totalCost}."));
            }
            else
            {
                // Fallback for non-grid movement
                if (source.Movement.MovementRemaining < _distance)
                {
                    return Result<ActionResult>.Failure($"Not enough movement. Required: {_distance}, Remaining: {source.Movement.MovementRemaining}");
                }
                source.Movement.Move(_distance);
                return Result<ActionResult>.Success(new ActionResult(true, $"Moved {_distance} feet (Abstract)."));
            }
        }

        private void ResolveOpportunityAttack(ICreature attacker, ICreature target, IGridManager grid)
        {
            if (_diceRoller == null) return; // Can't roll without roller

            // Create Attack Action
            // We assume a basic melee attack.
            // We need weapon info.
            string damageDice = "1d4"; // Default unarmed
            DamageType damageType = DamageType.Bludgeoning;
            
            if (attacker.Equipment?.MainHand != null)
            {
                damageDice = attacker.Equipment.MainHand.DamageDice;
                damageType = attacker.Equipment.MainHand.DamageType;
            }
            
            // Construct temporary action
            // Note: This bypasses the creature's own action list, which is a simplification.
            // We pass ActionType.Reaction so AttackAction handles the resource consumption.
            int reach = grid.GetReach(attacker);
            var attackAction = new AttackAction("Opportunity Attack", "Reaction Attack", 5, damageDice, damageType, 1, _diceRoller, ActionType.Reaction, reach);
            
            var context = new StandardActionContext(attacker, new CreatureTarget(target), grid);
            attackAction.Execute(context);
        }
    }
}
