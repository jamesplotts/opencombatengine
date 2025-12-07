using System;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Spatial;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Core.Models.Actions;

namespace OpenCombatEngine.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== OpenCombatEngine Event System Demo ===");
            Console.WriteLine();

            // 1. Setup Grid
            var grid = new StandardGridManager();
            var logger = new CombatLogger(grid);

            // 2. Create Creatures
            var player = CreateCreature("Hero", "Player", 16, 20); // Strong, 20 HP
            var goblin = CreateCreature("Goblin", "Monster", 10, 10); // Weak, 10 HP

            logger.RegisterCreatureWithContext(player);
            logger.RegisterCreatureWithContext(goblin);

            // 3. Place on Grid
            grid.PlaceCreature(player, new Position(0, 0, 0));
            grid.PlaceCreature(goblin, new Position(2, 0, 0)); // 10ft Away

            Console.WriteLine("Scenario: Hero approaches Goblin and attacks.");
            Console.WriteLine("---------------------------------------------");

            // 4. Move Hero (Trigger Movement Event)
            // Hero only needs to be adjacent (1,0,0) to attack with unarmed strike (Reach 5).
            // Current pos: (0,0,0). Goblin: (2,0,0).
            // Move to (1,0,0). Distance to goblin = 5ft.
            var movePos = new Position(1, 0, 0);
            
            // We can use grid.MoveCreature directly or use PerformAction(MoveAction).
            // Let's use MoveAction for full event chain.
            // MoveAction requires a context with valid Target (PositionTarget).
            
            var moveAction = new MoveAction(30);
            // MoveAction.Execute logic usually prompts user or takes a target?
            // Wait, Standard MoveAction implementation:
            // "if (context.Target is PositionTarget pt)..."
            
            var moveContext = new StandardActionContext(
                player,
                new OpenCombatEngine.Core.Models.Actions.PositionTarget(movePos),
                grid
            );

            player.PerformAction(moveAction, moveContext);

            // 5. Hero Attacks Goblin
            Console.WriteLine();
            Console.WriteLine("Hero takes a swing...");
            
            // Find Unarmed Strike or add a weapon.
            // StandardCreature creates "Unarmed Strike" in its Actions list.
            var attackAction = player.Actions.FirstOrDefault(a => a.Name == "Unarmed Strike");
            if (attackAction != null)
            {
                 var attackContext = new StandardActionContext(
                    player,
                    new OpenCombatEngine.Core.Models.Actions.CreatureTarget(goblin),
                    grid
                 );
                 
                 player.PerformAction(attackAction, attackContext);
            }
            else
            {
                Console.WriteLine("Error: No attack action found!");
            }

            // 6. Goblin runs away (Trigger Opportunity Attack)
            Console.WriteLine();
            Console.WriteLine("Goblin tries to flee! (Should trigger Opportunity Attack)");
            
            // Move Goblin from (2,0,0) to (3,0,0). Leaving Reach (5ft) of Hero at (1,0,0).
            var fleePos = new Position(3, 0, 0);
            
            // Direct grid move for simplicity, simulating AI 'Move' decision.
            // GridManager.MoveCreature triggers the core event + Reaction system.
            // This bypasses 'PerformAction' events for the movement itself, but still triggers 'Moved' and 'Reaction'.
            // Note: If Goblin uses Disengage action, it would suppress OA. We assume standard move.
            
            grid.MoveCreature(goblin, fleePos);

            Console.WriteLine();
            Console.WriteLine("=== Demo Complete ===");
        }

        static StandardCreature CreateCreature(string name, string team, int str, int hpMax)
        {
            var stats = new StandardAbilityScores(str, 10, 10, 10, 10, 10);
            var hp = new StandardHitPoints(hpMax);
            var inventory = new StandardInventory();
            var turnManager = new OpenCombatEngine.Implementation.StandardTurnManager(new StandardDiceRoller());
            
            var c = new StandardCreature(
                Guid.NewGuid().ToString(),
                name,
                stats,
                hp,
                inventory,
                turnManager
            );
            c.Team = team;
            return c;
        }
    }
}
