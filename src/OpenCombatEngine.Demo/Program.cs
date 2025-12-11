using System;
using System.Linq;
using OpenCombatEngine.Core.Interfaces;
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
using OpenCombatEngine.Implementation.Combat;
using OpenCombatEngine.Core.Interfaces.Combat;

namespace OpenCombatEngine.Demo
{
    class Program
    {
        static IGridManager? _grid;
        static StandardCreature? _player;
        static StandardCreature? _goblin;
        static bool _encounterRunning = true;

        static void Main(string[] args)
        {
            Console.WriteLine("=== OpenCombatEngine Event System Demo ===");
            Console.WriteLine();

            // 1. Setup Grid & Logger
            _grid = new StandardGridManager();
            var logger = new CombatLogger(_grid); // Preserves existing logger logic

            // 2. Setup Combat Manager & TurnManager
            var turnManager = new OpenCombatEngine.Implementation.StandardTurnManager(new StandardDiceRoller());
            
            // 3. Create Creatures (Injecting TurnManager)
            _player = CreateCreature("Hero", "Player", 16, 20, turnManager);
            _goblin = CreateCreature("Goblin", "Monster", 10, 10, turnManager);

            // Register Logger
            logger.RegisterCreatureWithContext(_player);
            logger.RegisterCreatureWithContext(_goblin);

            // 4. Place on Grid
            _grid.PlaceCreature(_player, new Position(0, 0, 0));
            _grid.PlaceCreature(_goblin, new Position(2, 0, 0)); // 10ft Away

            var combatManager = new StandardCombatManager(turnManager, _grid);
            
            // Subscribe to Turn Events to drive AI/Script
            turnManager.TurnChanged += OnTurnChanged;
            combatManager.EncounterEnded += OnEncounterEnded;

            Console.WriteLine("Starting Encounter...");
            combatManager.StartEncounter(new[] { _player, _goblin });

            // Game Loop Simulation (Console waits, but events drive it)
            // In a real app, this would be a loop. For console event-driven, we wait.
            // But StartEncounter triggers the first turn synchronously in current implementation?
            // Yes, StartCombat -> NextTurn -> StartTurn -> Event.
            // So if we script actions in the event handler, they will execute.
            // Be careful of recursion depth if everything is synchronous.
            // For this demo, recursion is fine for 2 turns.

            Console.WriteLine("=== Demo Complete ===");
        }

        static void OnTurnChanged(object? sender, OpenCombatEngine.Core.Models.Events.TurnChangedEventArgs e)
        {
            if (!_encounterRunning) return;
            if (e.Round > 10) 
            {
                 Console.WriteLine("Safety Limit Reached (10 Rounds). Stopping.");
                 _encounterRunning = false;
                 return;
            }

            var creature = e.Creature as StandardCreature;
            if (creature == null) return;

            Console.WriteLine($"\n--- Turn {e.Round}: {creature.Name} (HP: {creature.HitPoints.Current}) ---");

            if (creature == _player)
            {
                RunPlayerTurn();
            }
            else if (creature == _goblin)
            {
                RunGoblinTurn();
            }
            
            if (_encounterRunning)
            {
                ((OpenCombatEngine.Implementation.StandardTurnManager)sender).NextTurn();
            }
        }

        static void RunPlayerTurn()
        {
            if (_player == null || _goblin == null || _grid == null) return;

            // Simple Chase AI: Move adjacent to Goblin
            var playerPosNullable = _grid.GetPosition(_player);
            var goblinPosNullable = _grid.GetPosition(_goblin);
            
            if (!playerPosNullable.HasValue || !goblinPosNullable.HasValue) return;

            var playerPos = playerPosNullable.Value;
            var goblinPos = goblinPosNullable.Value;
            
            // Calc distance
            int dist = Math.Max(Math.Abs(playerPos.X - goblinPos.X), Math.Abs(playerPos.Y - goblinPos.Y)); // Max axis dist (Chebyshev)
            
            if (dist > 1)
            {
                // Move towards goblin
                // Vector
                int dx = goblinPos.X - playerPos.X;
                int dy = goblinPos.Y - playerPos.Y;
                
                // Normalize to 1 step
                int stepX = dx != 0 ? Math.Sign(dx) : 0;
                int stepY = dy != 0 ? Math.Sign(dy) : 0;
                
                // If dist > 1, we can move closer without stepping ON them (unless dist=1)
                // New pos
                var movePos = new Position(playerPos.X + stepX * (dist - 1), playerPos.Y, playerPos.Z); 
                // Just move 1 square towards them for simplicity if Speed allows
                // Let's just move to (Goblin.X - 1)
                movePos = new Position(goblinPos.X - 1, goblinPos.Y, goblinPos.Z);
                
                Console.WriteLine($"Hero moves to {movePos.X},{movePos.Y}...");
                var moveAction = new MoveAction(30);
                var moveContext = new StandardActionContext(_player, new PositionTarget(movePos), _grid);
                _player.PerformAction(moveAction, moveContext);
            }

            var attackAction = _player.Actions.FirstOrDefault(a => a.Name == "Unarmed Strike");
            if (attackAction != null)
            {
                 Console.WriteLine("Hero attacks!");
                 var attackContext = new StandardActionContext(_player, new CreatureTarget(_goblin), _grid);
                 _player.PerformAction(attackAction, attackContext);
            }
        }

        static void RunGoblinTurn()
        {
            if (_player == null || _goblin == null || _grid == null) return;
            
            // Flee logic: Run away from Player
            // For demo, just sit there and take it, or move once to trigger OA then stop?
            // If it keeps fleeing, Hero chases forever.
            // Let's have Goblin attack back if adjacent, or flee if low HP.
            // Simplest: Goblin just stands and fights to ensure demo ends efficiently.
            
            Console.WriteLine("Goblin sneers and attacks!");
             var attackAction = _goblin.Actions.FirstOrDefault(a => a.Name == "Unarmed Strike");
            if (attackAction != null)
            {
                 var attackContext = new StandardActionContext(_goblin, new CreatureTarget(_player), _grid);
                 _goblin.PerformAction(attackAction, attackContext);
            }
        }

        static void OnEncounterEnded(object? sender, EncounterEndedEventArgs e)
        {
            _encounterRunning = false;
            Console.WriteLine($"\n>>> Encounter Ended! Winner: {e.WinningTeam} <<<");
        }

        static StandardCreature CreateCreature(string name, string team, int str, int hpMax, ITurnManager turnManager)
        {
            var stats = new StandardAbilityScores(str, 10, 10, 10, 10, 10);
            var hp = new StandardHitPoints(hpMax);
            var inventory = new StandardInventory();
            
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
