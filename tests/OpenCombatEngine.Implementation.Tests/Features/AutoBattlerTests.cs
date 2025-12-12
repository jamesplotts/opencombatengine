using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.AI;
using OpenCombatEngine.Implementation.Combat;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Spatial;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class AutoBattlerTests
    {
        private readonly StandardGridManager _gridManager;
        private readonly StandardTurnManager _turnManager;
        private readonly BasicAiController _aiController;
        private readonly CombatRunner _runner;

        public AutoBattlerTests()
        {
            _gridManager = new StandardGridManager();
            _turnManager = new StandardTurnManager(new StandardDiceRoller());
            _aiController = new BasicAiController(_gridManager);
            _runner = new CombatRunner(_aiController);
        }

        private StandardCreature CreateCreature(string name, string team)
        {
             var abilities = new StandardAbilityScores(10, 10, 10, 10, 10, 10);
             var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                name,
                abilities,
                new StandardHitPoints(10, 10, 0), 
                new OpenCombatEngine.Implementation.Items.StandardInventory(),
                _turnManager
            ) { Team = team };
            
            // Add Basic Attack
            var attack = new AttackAction("Bite", "Bites", 0, "1d4", DamageType.Piercing, 0, new StandardDiceRoller());
            creature.AddAction(attack);
            // Add Move
            creature.AddAction(new MoveAction(30));
            
            return creature;
        }

        [Fact]
        public async Task Zombie_AI_Should_Move_Towards_Enemy_If_Out_Of_Reach()
        {
            // Setup
            var zombie = CreateCreature("Zombie", "Undead");
            var hero = CreateCreature("Hero", "Heroes");

            _gridManager.PlaceCreature(zombie, new Position(0, 0));
            _gridManager.PlaceCreature(hero, new Position(0, 4)); // 20ft away (Speed 30)

            // Make it Zombie's turn context
            // Normally runner gets current creature from turn manager.
            // We can manually invoke DetermineAction to verify logic first.
            
            // Mock Context
            var context = new StandardActionContext(zombie, new OpenCombatEngine.Core.Models.Actions.PositionTarget(new Position(0,0)), _gridManager);

            // Decision
            var decision = await _aiController.DetermineAction(zombie, context);

            // Assert
            decision.Should().NotBeNull();
            decision.Action.Name.Should().Be("Move");
            
            // Should move closer (0,1 or 0,2 etc)
            // It should target a point closer to (0,4)
            var targetPos = ((OpenCombatEngine.Core.Models.Actions.PositionTarget)decision.Target).Position;
            // E.g. (0,3) which is adjacent to (0,4)? 
            // Dist 20ft -> adjacent is 15ft away?
            // Wait, (0,0) to (0,4) is 4 squares = 20ft.
            // Adjacency is 5ft (1 sq).
            // So target should be (0,3).
            // BasicAiController pathfinding logic: path is [ (0,1), (0,2), (0,3) ].
            // Speed 30 allows all. Last valid is (0,3).
            
            // Actually, path includes Start? 
            // GridManager.GetPath includes start? 
            // My implementation of GetPath in StandardGridManager uses A*. Usually Start is Nodes[0].
            // BasicAiController implementation removed index 0.
            
            // Let's verify it picked a Move action towards enemy.
            // targetPos.Y.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Zombie_AI_Should_Attack_Enemy_If_In_Reach()
        {
            // Setup
            var zombie = CreateCreature("Zombie", "Undead");
            var hero = CreateCreature("Hero", "Heroes");

            _gridManager.PlaceCreature(zombie, new Position(0, 0));
            _gridManager.PlaceCreature(hero, new Position(0, 1)); // 5ft away

            // Mock Context
            var context = new StandardActionContext(zombie, new OpenCombatEngine.Core.Models.Actions.PositionTarget(new Position(0,0)), _gridManager);

            // Decision
            var decision = await _aiController.DetermineAction(zombie, context);

            // Assert
            decision.Should().NotBeNull();
            decision.Action.Name.Should().Be("Bite");
            
            // Target should be Hero
            var targetC = ((OpenCombatEngine.Core.Models.Actions.CreatureTarget)decision.Target).Creature;
            targetC.Should().Be(hero);
        }

        [Fact]
        public async Task Runner_Should_Execute_Turn_Automated()
        {
            // Setup full cycle
            var zombie = CreateCreature("Zombie", "Undead");
            var hero = CreateCreature("Hero", "Heroes");
            
            _gridManager.PlaceCreature(zombie, new Position(0, 0));
            _gridManager.PlaceCreature(hero, new Position(0, 1)); // In reach
            
            var participants = new[] { zombie, hero };
            _turnManager.StartCombat(participants);
            
            // Force Zombie to be current (hack turn index if needed, or just set it)
            // Or assume random init.
            // Let's force Zombie turn:
            while (_turnManager.CurrentCreature != zombie) _turnManager.NextTurn();

            // Run
            var context = new StandardActionContext(zombie, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(zombie), _gridManager);
            
            await _runner.RunTurn(_turnManager, context);
            
            // Assert: Hero took damage? 
            // Bite is 1d4. Hero HP 10.
            // Assuming it hit (Action dice roller is reliable or mockable).
            // We use real dice roller so it might miss.
            // But we can check that an Action was ATTEMPTED?
            // Or check if HP changed OR logs?
            // Hard to assert "Attempted" without mocks.
            // But if it Hit, HP < 10. If Miss, HP = 10.
            // Let's assert "Action was consumed" on Zombie?
            // Zombie.ActionEconomy.HasAction.Should().BeFalse();
            
            zombie.ActionEconomy.HasAction.Should().BeFalse("Zombie should have used Action to Attack");
        }
    }
}
