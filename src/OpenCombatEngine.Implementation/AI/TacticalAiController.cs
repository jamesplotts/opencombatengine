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
    public class TacticalAiController : IAiController
    {
        private readonly IGridManager _gridManager;

        public TacticalAiController(IGridManager gridManager)
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

            // 2. Self Preservation Check (Wisdom)
            // If Wisdom is high (> 12), and HP is low (< 30%), try to Disengage or Run.
            int wisdom = creature.AbilityScores.Wisdom;
            double hpPercent = (double)creature.HitPoints.Current / creature.HitPoints.Max;

            if (wisdom > 12 && hpPercent < 0.3)
            {
                // Preservation Mode
                // Priority: Disengage (if in melee), then Move (Run away).
                // Or just Move away if not engaged?
                // Let's implement Fleeing: Move AWAY from nearest enemy.
                
                // For now, let's keep it simple: If we possess a "Disengage" or "Dash" action, use it to get away?
                // Or just use Move action to run away.
                
                // For Tier 2, we will attempt to Move AWAY.
                var moveAction = creature.Actions.FirstOrDefault(a => a.Name == "Move");
                if (moveAction != null)
                {
                    // Find generic "Away" vector?
                    // Average position of enemies?
                    // Nearest enemy is biggest threat.
                    var nearestThreat = GetNearest(creature, enemies);
                    if (nearestThreat != null)
                    {
                        var threatPos = context.Grid.GetPosition(nearestThreat);
                        var myPos = context.Grid.GetPosition(creature);
                         
                        if (threatPos != null && myPos != null)
                        {
                            // Vector Threat -> Me
                            int dx = myPos.Value.X - threatPos.Value.X;
                            int dy = myPos.Value.Y - threatPos.Value.Y;
                            int dz = myPos.Value.Z - threatPos.Value.Z;
                            
                            // Normalize somewhat (just direction) and Multiply by speed?
                            // Simple: Try to move to (MyX + dx, MyY + dy). Avoid obstacles?
                            // Let's pick a point 30ft (6 squares) away in that direction.
                            
                            var fleeTarget = new Position(
                                myPos.Value.X + Math.Sign(dx) * 6,
                                myPos.Value.Y + Math.Sign(dy) * 6,
                                myPos.Value.Z + Math.Sign(dz) * 6
                            );
                            
                            // Clamp path/find valid path logic is inside MoveAction usually, 
                            // but MoveAction takes a destination.
                            // We should probably rely on Pathfinding to get us *towards* that flee target.
                            
                            return Task.FromResult<AiDecision?>(
                                new AiDecision(moveAction, new PositionTarget(fleeTarget))
                            );
                        }
                    }
                }
            }

            // 3. Target Selection (Intelligence)
            // Score targets.
            ICreature? bestTarget = null;
            double bestScore = -1;

            int intelligence = creature.AbilityScores.Intelligence;
            
            foreach (var enemy in enemies)
            {
                double score = 0;
                
                // Distance Score (Closer is better)
                int dist = _gridManager.GetDistance(creature, enemy);
                score += (100.0 / (dist + 5)); // +5 to avoid div/0 and flatten slightly
                
                // HP Score (Lower HP % is better if Smart)
                if (intelligence >= 8)
                {
                    double enemyHpPct = (double)enemy.HitPoints.Current / enemy.HitPoints.Max;
                    score += (1 - enemyHpPct) * 50; // Up to 50 points for low HP
                }
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = enemy;
                }
            }
            
            if (bestTarget == null) bestTarget = enemies.First(); // Fallback

            // 4. Engage Best Target
            // Check Reach
            int reach = _gridManager.GetReach(creature);
            int distToTarget = _gridManager.GetDistance(creature, bestTarget);
            
            if (distToTarget <= reach)
            {
                // Attack
                var attackAction = creature.Actions.FirstOrDefault(a => 
                    a.Name.Contains("Attack", StringComparison.OrdinalIgnoreCase) || 
                    a.Name.Contains("Bite", StringComparison.OrdinalIgnoreCase));
                    
                if (attackAction != null)
                {
                    return Task.FromResult<AiDecision?>(new AiDecision(attackAction, new CreatureTarget(bestTarget)));
                }
            }
            else
            {
                // Move
                var moveAction = creature.Actions.FirstOrDefault(a => a.Name == "Move");
                if (moveAction != null)
                {
                     var targetPos = context.Grid.GetPosition(bestTarget);
                     if (targetPos != null)
                     {
                         // Pathfind to it (GridManager handles pathing in MoveAction logic? 
                         // No, MoveAction expects a destination. We give the Enemy Position, 
                         // knowing we can't accept it, but BasicAiController gave the *End of Path*.
                         // We should calculate the path step here like BasicAiController did?
                         // Yes, for robustness.
                         
                         var myPos = context.Grid.GetPosition(creature);
                         if (myPos != null)
                         {
                             var path = context.Grid.GetPath(myPos.Value, targetPos.Value).ToList();
                             if (path.Count > 0) path.RemoveAt(0); // Removing start
                             if (path.Count > 0 && context.Grid.GetCreatureAt(path.Last()) != null) path.RemoveAt(path.Count - 1); // Remove occupied dest
                             
                             if (path.Count > 0)
                             {
                                 int maxSteps = 6; // Speed 30
                                 var dest = path.Take(maxSteps).LastOrDefault();
                                 return Task.FromResult<AiDecision?>(new AiDecision(moveAction, new PositionTarget(dest)));
                             }
                         }
                     }
                }
            }

            return Task.FromResult<AiDecision?>(null);
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
    }
}
