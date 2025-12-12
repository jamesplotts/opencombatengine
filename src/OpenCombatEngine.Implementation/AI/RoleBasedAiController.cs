using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.AI;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.AI;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Implementation.AI
{
    public class RoleBasedAiController : IAiController
    {
        private readonly IGridManager _gridManager;

        public RoleBasedAiController(IGridManager gridManager)
        {
            _gridManager = gridManager ?? throw new ArgumentNullException(nameof(gridManager));
        }

        public Task<AiDecision?> DetermineAction(ICreature creature, IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(creature);
            ArgumentNullException.ThrowIfNull(context);

            if (context.Grid == null) return Task.FromResult<AiDecision?>(null);

            // 1. Identify Enemies
            var allCreatures = context.Grid.GetAllCreatures();
            var enemies = allCreatures.Where(c => c.Team != creature.Team && !c.HitPoints.IsDead).ToList();

            if (enemies.Count == 0) return Task.FromResult<AiDecision?>(null);

            // 2. Self Preservation (Wisdom - Tier 2)
            int wisdom = creature.AbilityScores.Wisdom;
            double hpPercent = (double)creature.HitPoints.Current / creature.HitPoints.Max;

            if (wisdom > 12 && hpPercent < 0.3)
            {
                // Flee
                var fleeDecision = CreateFleeDecision(creature, enemies, context);
                if (fleeDecision != null) return Task.FromResult<AiDecision?>(fleeDecision);
            }

            // 3. Target Selection (Intelligence - Tier 2)
            var bestTarget = SelectBestTarget(creature, enemies);
            if (bestTarget == null) return Task.FromResult<AiDecision?>(null);

            // 4. Role Logic (Tier 3)
            if (creature.Tags.Contains("Role:Artillery"))
            {
                return Task.FromResult(HandleArtilleryLogic(creature, bestTarget, context));
            }
            
            // Fallback to Tactical/Zombie Logic (Tier 2/1)
            return Task.FromResult(HandleMeleeLogic(creature, bestTarget, context));
        }

        private AiDecision? HandleArtilleryLogic(ICreature creature, ICreature target, IActionContext context)
        {
            // Artillery: Stay at range.
            // Find Ranged Attack
            var attackAction = creature.Actions.FirstOrDefault(a => 
                (a.Name.Contains("Bow", StringComparison.OrdinalIgnoreCase) || a.Name.Contains("Crossbow", StringComparison.OrdinalIgnoreCase) || a.Name.Contains("Ray", StringComparison.OrdinalIgnoreCase) || a is OpenCombatEngine.Implementation.Actions.CastSpellAction) 
                && !a.Name.Contains("Melee", StringComparison.OrdinalIgnoreCase)); // Heuristic
            
            // Or use Open5e "Range" from description if we parsed it? We didn't parse it into action model.
            // But we inferred Role based on it.
            // Let's assume the action that *qualified* us for Artillery is available.
            
            // Ideally we'd scan actions for largest range.
            // But we don't have Range in IAction interface uniformly.
            // We'll trust "AttackAction" or assume generic "Ranged Attack".
            
            // Let's define "Optimal Range" as 30-60ft.
            int optimalDist = 30; // 6 squares
            int currentDist = _gridManager.GetDistance(creature, target);
            
            var moveAction = creature.Actions.FirstOrDefault(a => a.Name == "Move");
            
            if (currentDist < optimalDist)
            {
                // Too close! Kite! (Move Away)
                if (moveAction != null)
                {
                     var fleePos = CalculateFleePosition(creature, target, context);
                     return new AiDecision(moveAction, new PositionTarget(fleePos));
                }
            }
            
            // If range is okay (>= Optimal), Check if we can attack.
            // Need to know if we are in range of weapon.
            // Assuming 60ft range for Artillery.
            int maxRange = 60; 
            
            if (currentDist <= maxRange)
            {
                // Attack!
                // Prioritize Ranged Actions
                var action = creature.Actions.FirstOrDefault(a => 
                    a.Name.Contains("Bow", StringComparison.OrdinalIgnoreCase) || 
                    a.Name.Contains("Crossbow", StringComparison.OrdinalIgnoreCase) ||
                    a.Name.Contains("Ray", StringComparison.OrdinalIgnoreCase) ||
                    a is OpenCombatEngine.Implementation.Actions.CastSpellAction);
                
                // Fallback to any attack if no ranged found (shouldn't happen if tagged properly)
                if (action == null)
                {
                     action = creature.Actions.FirstOrDefault(a => 
                        a.Name.Contains("Attack", StringComparison.OrdinalIgnoreCase) || 
                        a is OpenCombatEngine.Implementation.Actions.AttackAction);
                }
                if (action != null)
                {
                    return new AiDecision(action, new CreatureTarget(target));
                }
            }
            else
            {
                // Too far? Move closer.
                if (moveAction != null)
                {
                    // Move towards target
                    var targetPos = context.Grid?.GetPosition(target);
                    if (targetPos != null)
                    {
                         // Basic move towards
                         // Simplified: We rely on MoveAction pathfinding for "Towards"
                         // But we need to give a destination.
                         return new AiDecision(moveAction, new PositionTarget(targetPos.Value));
                    }
                }
            }
            
            return null;
        }

        private AiDecision? HandleMeleeLogic(ICreature creature, ICreature target, IActionContext context)
        {
            // Copied from TacticalAiController (Tier 2 behavior)
            int reach = _gridManager.GetReach(creature);
            int distToTarget = _gridManager.GetDistance(creature, target);
            
            if (distToTarget <= reach)
            {
                var attackAction = creature.Actions.FirstOrDefault(a => 
                    a.Name.Contains("Attack", StringComparison.OrdinalIgnoreCase) || 
                    a.Name.Contains("Bite", StringComparison.OrdinalIgnoreCase) ||
                    a is OpenCombatEngine.Implementation.Actions.AttackAction);
                    
                if (attackAction != null)
                {
                    return new AiDecision(attackAction, new CreatureTarget(target));
                }
            }
            else
            {
                var moveAction = creature.Actions.FirstOrDefault(a => a.Name == "Move");
                if (moveAction != null)
                {
                     var targetPos = context.Grid?.GetPosition(target);
                     if (targetPos != null)
                     {
                         // Simplified path calculation to avoid re-implementing full A* loop here
                         // We rely on GridManager to find next step if we pass the target?
                         // No, MoveAction takes a destination and moves *towards* it.
                         // So passing targetPos is usually fine, MoveAction stops at reach?
                         // MoveAction stops when it runs out of movement or hits obstacle.
                         // But if we want to stop ADJACENT, passing targetPos (occupied) might fail validation in MoveAction?
                         // "if (IsBlocked(neighbor) && !neighbor.Equals(destination)) continue;" -> In GridManager
                         // If neighbor == destination and it is Blocked (by creature), A* returns it as valid node?
                         // Yes, usually.
                         // But MoveAction checks: "if (!moveResult.IsSuccess) return Failure".
                         // Grid.MoveCreature fails if dest is occupied.
                         // So we must NOT try to move ONTO the target.
                         // We must find an adjacent square.
                         
                         // Hack: Find free adjacent square to target.
                         foreach(var adj in GetNeighbors(targetPos.Value))
                         {
                             if (_gridManager.GetCreatureAt(adj) == null && !_gridManager.IsObstructed(adj))
                             {
                                 return new AiDecision(moveAction, new PositionTarget(adj));
                             }
                         }
                         
                         // If no adjacent free, try targetPos anyway (maybe logic handles it later?)
                         // Or just don't move.
                     }
                }
            }
            return null;
        }

        private ICreature? SelectBestTarget(ICreature creature, List<ICreature> enemies)
        {
            // Intelligence Logic
            ICreature? bestTarget = null;
            double bestScore = -1;
            int intelligence = creature.AbilityScores.Intelligence;
            
            foreach (var enemy in enemies)
            {
                double score = 0;
                int dist = _gridManager.GetDistance(creature, enemy);
                score += (100.0 / (dist + 5));
                
                if (intelligence >= 8)
                {
                    double enemyHpPct = (double)enemy.HitPoints.Current / enemy.HitPoints.Max;
                    score += (1 - enemyHpPct) * 50;
                }
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }
            return bestTarget ?? enemies.FirstOrDefault();
        }

        private AiDecision? CreateFleeDecision(ICreature creature, List<ICreature> enemies, IActionContext context)
        {
             var moveAction = creature.Actions.FirstOrDefault(a => a.Name == "Move");
             if (moveAction != null)
             {
                 var nearestThreat = GetNearest(creature, enemies);
                 if (nearestThreat != null)
                 {
                     var fleePos = CalculateFleePosition(creature, nearestThreat, context);
                     return new AiDecision(moveAction, new PositionTarget(fleePos));
                 }
             }
             return null;
        }
        
        private static Position CalculateFleePosition(ICreature me, ICreature threat, IActionContext context)
        {
            var threatPos = context.Grid?.GetPosition(threat);
            var myPos = context.Grid?.GetPosition(me);
             
            if (threatPos != null && myPos != null)
            {
                int dx = myPos.Value.X - threatPos.Value.X;
                int dy = myPos.Value.Y - threatPos.Value.Y;
                int dz = myPos.Value.Z - threatPos.Value.Z;
                
                // Move 30ft (6 squares) away
                return new Position(
                    myPos.Value.X + Math.Sign(dx) * 6,
                    myPos.Value.Y + Math.Sign(dy) * 6,
                    myPos.Value.Z + Math.Sign(dz) * 6
                );
            }
            return myPos ?? new Position(0,0);
        }

        private ICreature? GetNearest(ICreature me, IEnumerable<ICreature> others)
        {
            ICreature? nearest = null;
            int minDist = int.MaxValue;
            foreach(var o in others)
            {
                int d = _gridManager.GetDistance(me, o);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = o;
                }
            }
            return nearest;
        }
        
        private static IEnumerable<Position> GetNeighbors(Position center)
        {
            // Simple subset for efficiency
             yield return new Position(center.X + 1, center.Y, center.Z);
             yield return new Position(center.X - 1, center.Y, center.Z);
             yield return new Position(center.X, center.Y + 1, center.Z);
             yield return new Position(center.X, center.Y - 1, center.Z);
        }
    }
}
