using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.AI;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spatial;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class RoleAiTests
    {
        private readonly StandardGridManager _gridManager;
        private readonly StandardTurnManager _turnManager;
        private readonly RoleBasedAiController _controller;

        public RoleAiTests()
        {
            _gridManager = new StandardGridManager();
            _turnManager = new StandardTurnManager(new StandardDiceRoller());
            _controller = new RoleBasedAiController(_gridManager);
        }

        private StandardCreature CreateCreature(string name, int hp, string team, string role = "")
        {
             var abilities = new StandardAbilityScores(10, 10, 10, 10, 10, 10);
             var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                name,
                abilities,
                new StandardHitPoints(hp, hp, 0), 
                new StandardInventory(),
                _turnManager
            ) { Team = team };
            
            // Add Basic Moves
            creature.AddAction(new MoveAction(30));
            
            if (!string.IsNullOrEmpty(role))
            {
                creature.Tags = new List<string> { role };
            }
            
            return creature;
        }
        
        private void AddRangedAttack(StandardCreature creature)
        {
            var attack = new AttackAction(
                "Longbow", 
                "Ranged Attack", 
                5, 
                "1d8", 
                DamageType.Piercing, 
                0, 
                new StandardDiceRoller(), 
                ActionType.Action, 
                150 // Reach/Range
            );
            creature.AddAction(attack);
        }

        [Fact]
        public async Task Artillery_Should_Kite_If_Too_Close()
        {
            // Ranger (Artillery)
            var artillery = CreateCreature("Ranger", 20, "BadGuys", "Role:Artillery");
            AddRangedAttack(artillery);
            
            // Enemy (Melee)
            var enemy = CreateCreature("Fighter", 20, "Heroes");

            // Setup: Enemy is CLOSE (10ft)
            // Artillery Optimal Range is 30ft
            _gridManager.PlaceCreature(artillery, new Position(0, 0));
            _gridManager.PlaceCreature(enemy, new Position(0, 2)); // 10ft away

            var context = new StandardActionContext(artillery, new OpenCombatEngine.Core.Models.Actions.PositionTarget(new Position(0,0)), _gridManager);

            // Act
            var decision = await _controller.DetermineAction(artillery, context);

            // Assert
            decision.Should().NotBeNull();
            decision.Action.Name.Should().Be("Move");
            
            // Should move AWAY from (0,2).
            // Vector: Me(0,0) - Enemy(0,2) = (0, -2).
            // Should move towards (0, -X).
            var dest = ((OpenCombatEngine.Core.Models.Actions.PositionTarget)decision.Target).Position;
            
            dest.Y.Should().BeLessThan(0);
        }

        [Fact]
        public async Task Artillery_Should_Attack_If_At_Optimal_Range()
        {
            // Ranger (Artillery)
            var artillery = CreateCreature("Ranger", 20, "BadGuys", "Role:Artillery");
            AddRangedAttack(artillery);
            
            // Enemy (Melee)
            var enemy = CreateCreature("Fighter", 20, "Heroes");

            // Setup: Enemy is OPTIMAL (30ft)
            _gridManager.PlaceCreature(artillery, new Position(0, 0));
            _gridManager.PlaceCreature(enemy, new Position(0, 6)); // 30ft away

            var context = new StandardActionContext(artillery, new OpenCombatEngine.Core.Models.Actions.PositionTarget(new Position(0,0)), _gridManager);

            // Act
            var decision = await _controller.DetermineAction(artillery, context);

            // Assert
            decision.Should().NotBeNull();
            // Should Attack, not Move (since range is good)
            // Or maybe Move to Maintain? No, logic says "If current < Optimal: Kite. Else: Attack".
            
            decision.Action.Name.Should().Contain("Longbow");
        }
    }
}
