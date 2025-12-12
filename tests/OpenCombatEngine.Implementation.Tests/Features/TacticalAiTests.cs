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
    public class TacticalAiTests
    {
        private readonly StandardGridManager _gridManager;
        private readonly StandardTurnManager _turnManager;
        private readonly TacticalAiController _controller;

        public TacticalAiTests()
        {
            _gridManager = new StandardGridManager();
            _turnManager = new StandardTurnManager(new StandardDiceRoller());
            _controller = new TacticalAiController(_gridManager);
        }

        private StandardCreature CreateCreature(string name, int intScore, int wisScore, int hp, string team)
        {
             // Str, Dex, Con, Int, Wis, Cha
             var abilities = new StandardAbilityScores(10, 10, 10, intScore, wisScore, 10);
             var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                name,
                abilities,
                new StandardHitPoints(hp, hp, 0), 
                new StandardInventory(),
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
        public async Task Finisher_Behavior_Should_Select_Low_HP_Target_If_Int_High()
        {
            // Smart Attacker (Int 16)
            var attacker = CreateCreature("SmartAI", 16, 10, 20, "BadGuys");
            
            // Target A: Full HP (20/20), Close (5ft)
            // Target B: Critical HP (1/20), Farther (10ft)
            var targetA = CreateCreature("HealthyHero", 10, 10, 20, "Heroes");
            var targetB = CreateCreature("DyingHero", 10, 10, 20, "Heroes");
            targetB.HitPoints.TakeDamage(19, DamageType.Force); // Reduce to 1 HP

            _gridManager.PlaceCreature(attacker, new Position(0, 0));
            _gridManager.PlaceCreature(targetA, new Position(0, 1)); // 5ft
            _gridManager.PlaceCreature(targetB, new Position(0, 2)); // 10ft

            var context = new StandardActionContext(attacker, new OpenCombatEngine.Core.Models.Actions.PositionTarget(new Position(0,0)), _gridManager);

            // Act
            var decision = await _controller.DetermineAction(attacker, context);

            // Assert
            decision.Should().NotBeNull();
            // Should attack Target B because Int is high (Finisher score > Distance score penalty usually)
            // Let's check math:
            // Target A: Dist 5. Score = 100/10 + (1-1)*50 = 10 + 0 = 10.
            // Target B: Dist 10. Score = 100/15 + (1-0.05)*50 = 6.6 + 47.5 = 54.1.
            // 54 > 10. Should pick B.
            
            // Wait: If B is at (0,2), distance from (0,0) is 10. 
            // Wait: Is attacker in reach of B? B is at 10ft. Reach is usually 5ft.
            // If not in reach, it will Move towards B?
            // DetermineAction logic: 1. Pick Best Target. 2. If in reach, Attack. Else, Move.
            // So it should Move towards B.
            
            decision.Action.Name.Should().Be("Move");
            // Verify target is valid step closer to B
            // B is at (0,2).
            // Move Target should be adjacent to B (Distance 5).
            _gridManager.GetDistance(moveTarget, new Position(0,2)).Should().Be(5);
            
            // And should be adjacent to Start (0,0) (since speed allows it)
            _gridManager.GetDistance(moveTarget, new Position(0,0)).Should().Be(5);
        }

        [Fact]
        public async Task Zombie_Behavior_Should_Select_Closest_Target_If_Int_Low()
        {
            // Dumb Attacker (Int 4)
            var attacker = CreateCreature("DumbAI", 4, 10, 20, "BadGuys");
            
            // Target A: Full HP (20/20), Close (5ft)
            // Target B: Critical HP (1/20), Farther (10ft)
            var targetA = CreateCreature("HealthyHero", 10, 10, 20, "Heroes");
            var targetB = CreateCreature("DyingHero", 10, 10, 20, "Heroes");
            targetB.HitPoints.TakeDamage(19, DamageType.Force); // Reduce to 1 HP

            _gridManager.PlaceCreature(attacker, new Position(0, 0));
            _gridManager.PlaceCreature(targetA, new Position(0, 1)); // 5ft
            _gridManager.PlaceCreature(targetB, new Position(0, 2)); // 10ft

            var context = new StandardActionContext(attacker, new OpenCombatEngine.Core.Models.Actions.PositionTarget(new Position(0,0)), _gridManager);

            // Act
            var decision = await _controller.DetermineAction(attacker, context);

            // Assert
            // Math:
            // Target A: Dist 5. Score = 100/10 = 10.
            // Target B: Dist 10. Score = 100/15 = 6.6.
            // Should pick A.
            // A is in reach (5ft). Should Attack A.
            
            decision.Should().NotBeNull();
            decision.Action.Name.Should().Contain("Bite");
            var targetC = ((OpenCombatEngine.Core.Models.Actions.CreatureTarget)decision.Target).Creature;
            targetC.Should().Be(targetA);
        }

        [Fact]
        public async Task Self_Preservation_Should_Flee_If_Wis_High_And_HP_Low()
        {
            // Wise Attacker (Wis 16), Low HP
            var attacker = CreateCreature("WiseCoward", 10, 16, 20, "BadGuys");
            attacker.HitPoints.TakeDamage(15, DamageType.Force); // 5/20 HP (25%)

            var enemy = CreateCreature("ScaryHero", 10, 10, 20, "Heroes");
            
            _gridManager.PlaceCreature(attacker, new Position(0, 0));
            _gridManager.PlaceCreature(enemy, new Position(0, 1)); // Enemy is North (Y+)

            var context = new StandardActionContext(attacker, new OpenCombatEngine.Core.Models.Actions.PositionTarget(new Position(0,0)), _gridManager);

            // Act
            var decision = await _controller.DetermineAction(attacker, context);

            // Assert
            decision.Should().NotBeNull();
            decision.Action.Name.Should().Be("Move");
            
            // Should move AWAY from (0,1).
            // Vector: Me(0,0) - Enemy(0,1) = (0, -1).
            // Should move towards (0, -1).
            var dest = ((OpenCombatEngine.Core.Models.Actions.PositionTarget)decision.Target).Position;
            
            // Flee logic picks a target far away (6 sqs).
            // Destination is clamped by max steps?
            // "but MoveAction takes a destination... we rely on Pathfinding to get us *towards* that flee target"
            // Wait, TacticalAiController passes `fleeTarget` (which is far away) directly to `AiDecision`?
            // Or does it calculate the first step like Move logic?
            // In TacticalAiController implementation: `return ... new PositionTarget(fleeTarget)`.
            // It passes the FAR AWAY target.
            // The `MoveAction` logic will try to path to it.
            // `MoveAction` implementation: `var path = Grid.GetPath(...)`. `for(i=1; i<path.Count... limit by speed)`.
            // So if we pass a Far target, MoveAction will move as far as it can towards it.
            // So target in AiDecision is the Far Point.
            
            // Target should be roughly (0, -6).
            dest.Y.Should().BeLessThan(0);
        }
    }
}
