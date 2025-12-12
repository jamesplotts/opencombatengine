using System;
using System.Linq;
using System.Threading.Tasks;
using OpenCombatEngine.Core.Interfaces.AI;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions; // Added
using System.Collections.Generic;

namespace OpenCombatEngine.Implementation.AI
{
    public class BasicAiController : IAiController
    {
        private readonly IGridManager _gridManager;

        public BasicAiController(IGridManager gridManager)
        {
            _gridManager = gridManager ?? throw new ArgumentNullException(nameof(gridManager));
        }

        public Task<OpenCombatEngine.Core.Models.AI.AiDecision?> DetermineAction(ICreature creature, IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(creature);
            ArgumentNullException.ThrowIfNull(context);

            if (context.Grid == null) return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(null);

            // 1. Identify Enemies
            var allCreatures = context.Grid.GetAllCreatures();
            var enemies = allCreatures.Where(c => c.Team != creature.Team && !c.HitPoints.IsDead).ToList();

            if (enemies.Count == 0) return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(null);

            // 2. Find Nearest Enemy
            ICreature? nearestEnemy = null;
            double minDistance = double.MaxValue;

            var myPos = context.Grid.GetPosition(creature);
            if (myPos == null) return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(null);

            foreach (var enemy in enemies)
            {
                var enemyPos = context.Grid.GetPosition(enemy);
                if (enemyPos == null) continue;
                
                var dist = context.Grid.GetDistance(myPos.Value, enemyPos.Value);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestEnemy = enemy;
                }
            }

            if (nearestEnemy == null) return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(null);

            // 3. Attack or Move
            // Reach check (Standard 5ft + Weapon reach?)
            // Assuming 5ft for Basic AI zombie
            int reach = 5; 
            
            if (minDistance <= reach)
            {
                // Attack
                var attackAction = creature.Actions.FirstOrDefault(a => 
                    a.Name.Contains("Attack", StringComparison.OrdinalIgnoreCase) || 
                    a.Name.Contains("Bite", StringComparison.OrdinalIgnoreCase) || 
                    a.Name.Contains("Slam", StringComparison.OrdinalIgnoreCase)); // "Slam" for Zombies
                
                // Fallback: Pick ANY action that isn't Move/Dash
                if (attackAction == null)
                {
                    attackAction = creature.Actions.FirstOrDefault(a => a.Type == ActionType.Action && a.Name != "Move" && a.Name != "Dash");
                }

                if (attackAction != null)
                {
                     // Create Target Wrapper
                     // We need an IActionTarget that wraps the creature.
                     // Usually `CreatureTarget` class in `OpenCombatEngine.Core.Models.Actions`?
                     // I need to instantiate it.
                     var targetWrapper = new OpenCombatEngine.Core.Models.Actions.CreatureTarget(nearestEnemy);
                     return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(new OpenCombatEngine.Core.Models.AI.AiDecision(attackAction, targetWrapper));
                }
            }
            else
            {
                // Move
                var moveAction = creature.Actions.FirstOrDefault(a => a.Name == "Move");
                
                if (moveAction != null)
                {
                   // Create Position Target?
                   // MoveAction usually takes a Position (PointTarget?)
                   // But `DetermineAction` returns ONE decision.
                   // MoveAction usually moves to a specific square.
                   // So we need to calculate the path and pick the destination square (based on Speed).
                   
                   // Simplified Zombie Logic: Move directly towards enemy up to Speed.
                   // int speed = 30; // Default (Unused for now)
                   
                   // Find path to enemy
                   // GridManager uses A* usually.
                   // But `MoveAction` implementation might handle pathfinding if we give it a destination?
                   // Usually `MoveAction.Execute` attempts to move to the `Target` position.
                   // It validates if we can reach it.
                   // So the AI needs to pick a Valid Destination within Speed.
                   
                   var enemyPosVal = context.Grid.GetPosition(nearestEnemy);
                   if (enemyPosVal == null) return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(null);
                   var enemyPos = enemyPosVal.Value;
                   
                   // Get adjacent square to enemy (closest to me)
                   // Or just set target to enemyPos and let pathfinder stop short?
                   // Usually MoveAction fails if destination is occupied.
                   // So we find a free square adjacent to enemy.
                   
                   // Calculating destination...
                   // This logic is getting complex for "Basic" AI inside the controller.
                   // The "Zombie" just tries to get close.
                   // Let's rely on standard pathfinding?
                   // Assume we pick the enemy's location as desire, but we need to pick a valid empty square.
                   
                   // Cheat: Get Path to enemy. Take point at index [Speed].
                   // Accessing Path: context.Grid.GetPath(myPos, enemyPos)
                   // But `GetPath` is blocking/calculating.
                   
                   // Implementation:
                   // var path = context.Grid.GetPath(myPos.Value, enemyPos);
                   // The path includes start and end.
                   // If end is occupied (it is), we stop at last free node.
                   // Also limit by Speed.
                   
                   // Let's implement this path finding logic briefly.
                   // Assuming standard Speed=30 / 5 = 6 squares.
                   
                   var path = context.Grid.GetPath(myPos.Value, enemyPos).ToList();
                   // Path usually: [Start, Step1, Step2, ... End]
                   
                   // Remove Start
                   if (path.Count > 0) path.RemoveAt(0);

                   // Last step is Enemy (Occupied). Remove it.
                   if (path.Count > 0 && context.Grid.GetCreatureAt(path.Last()) != null) 
                   {
                       path.RemoveAt(path.Count - 1);
                   }
                   
                   if (path.Count > 0)
                   {
                       // Limit by max speed (e.g. 6 squares)
                       // Assume speed 6 for now (30ft)
                       int maxSteps = 6;
                       var dest = path.Take(maxSteps).LastOrDefault();
                       
                       var targetWrapper = new OpenCombatEngine.Core.Models.Actions.PositionTarget(dest);
                       return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(new OpenCombatEngine.Core.Models.AI.AiDecision(moveAction, targetWrapper));
                   }
                }
            }

            return Task.FromResult<OpenCombatEngine.Core.Models.AI.AiDecision?>(null);
        }
    }
}
