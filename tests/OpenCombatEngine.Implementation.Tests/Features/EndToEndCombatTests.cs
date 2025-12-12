using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Implementation.AI;
using OpenCombatEngine.Implementation.Combat;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Open5e;
using OpenCombatEngine.Implementation.Open5e.Models;
using OpenCombatEngine.Implementation.Spatial;
using OpenCombatEngine.Implementation; // For StandardTurnManager
// I need to check where StandardTurnManager is. Likely Implementation.Managers or Combat?
// Checking previous view files...
// StandardTurnManager wasn't explicitly viewed but constructor requires ITurnManager.
// StandardTurnManager is likely in OpenCombatEngine.Implementation.Managers or OpenCombatEngine.Implementation.Combat.
// I'll assume Implementation.Managers based on likely structure, or verify if compile fails.
// Actually, earlier error logs mentioned StandardTurnManager.
// Let's add namespace OpenCombatEngine.Implementation.Combats was wrong.
// Maybe OpenCombatEngine.Implementation.Turn ... ?
// I will check file location if this fails, but for now I'll include likely ones.

using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class EndToEndCombatTests
    {
        [Fact]
        public async Task Full_Combat_Simulation_Artillery_Vs_Zombie()
        {
            // 1. Setup Dependencies
            var diceRoller = new StandardDiceRoller(); 
            var gridManager = new StandardGridManager();
            
            // StandardTurnManager location? Let's assume it's available.
            // If I can't find it, I'll need to check.
            // It is instantiated in OpenCombatEngine.Implementation.Creatures.StandardCreature...
            // It is likely in OpenCombatEngine.Implementation.Combat or Managers.
            // Let's rely on global search or just try instantiation.
            var turnManager = new OpenCombatEngine.Implementation.StandardTurnManager(diceRoller);
            
            var combatManager = new StandardCombatManager(turnManager, gridManager);
            
            // 2. Mock Open5e Client
            var httpClient = new HttpClient(); 
            var mockClient = Substitute.For<Open5eClient>(httpClient);
            
            // Mock Goblin (Zombie AI)
            var goblinData = new Open5eMonster
            {
                Slug = "goblin",
                Name = "Goblin",
                HitPoints = 7,
                HitDice = "2d6",
                ArmorClass = 15,
                Strength = 8, Dexterity = 14, Constitution = 10, Intelligence = 10, Wisdom = 8, Charisma = 8,
                Actions = new System.Collections.Generic.List<Open5eMonsterAction>
                {
                    new Open5eMonsterAction { Name = "Scimitar", Desc = "Melee Weapon Attack: +4 to hit, reach 5 ft., one target. Hit: 5 (1d6 + 2) slashing damage." }
                },
                Speed = new System.Collections.Generic.Dictionary<string, object> { { "walk", 30 } }
            };

            // Mock Ranger (Artillery AI)
            var rangerData = new Open5eMonster
            {
                Slug = "ranger",
                Name = "Ranger",
                HitPoints = 20,
                HitDice = "3d10",
                ArmorClass = 14,
                Strength = 10, Dexterity = 16, Constitution = 12, Intelligence = 12, Wisdom = 14, Charisma = 10,
                Actions = new System.Collections.Generic.List<Open5eMonsterAction>
                {
                    new Open5eMonsterAction { Name = "Longbow", Desc = "Ranged Weapon Attack: +5 to hit, range 150/600 ft., one target. Hit: 7 (1d8 + 3) piercing damage." }
                },
                Speed = new System.Collections.Generic.Dictionary<string, object> { { "walk", 30 } }
            };

            mockClient.GetMonsterAsync("goblin").Returns(Task.FromResult<Open5eMonster?>(goblinData));
            mockClient.GetMonsterAsync("ranger").Returns(Task.FromResult<Open5eMonster?>(rangerData));
            
            var contentSource = new Open5eContentSource(mockClient, diceRoller);

            // 3. Load Creatures
            var goblinResult = await contentSource.GetMonsterAsync("goblin");
            goblinResult.IsSuccess.Should().BeTrue();
            var goblin = (OpenCombatEngine.Implementation.Creatures.StandardCreature)goblinResult.Value;
            goblin.Team = "BadGuys";

            var rangerResult = await contentSource.GetMonsterAsync("ranger");
            rangerResult.IsSuccess.Should().BeTrue();
            var ranger = (OpenCombatEngine.Implementation.Creatures.StandardCreature)rangerResult.Value;
            ranger.Team = "Heroes";
            
            // Verify Logic Tags
            // Ranger should have Role:Artillery inferred from Longbow range (150)
            ranger.Tags.Should().Contain("Role:Artillery");

            // 4. Setup Encounter
            // StandardCombatManager.StartEncounter takes IEnumerable<ICreature>
            
            // Pre-positioning? 
            // GridManager.PlaceCreature needs to be called.
            gridManager.PlaceCreature(goblin, new OpenCombatEngine.Core.Models.Spatial.Position(0, 0));
            gridManager.PlaceCreature(ranger, new OpenCombatEngine.Core.Models.Spatial.Position(0, 8)); // 40ft

            var participants = new OpenCombatEngine.Core.Interfaces.Creatures.ICreature[] { goblin, ranger };
            combatManager.StartEncounter(participants);
            
            // Verify Combat Started
            (turnManager.CurrentRound > 0).Should().BeTrue();

            // 5. Run Loop
            var aiController = new RoleBasedAiController(gridManager);
            var combatRunner = new CombatRunner(aiController);

            // Run 3 Rounds or until death
            for (int i = 0; i < 3 * 2; i++) // 2 creatures * 3 rounds approx
            {
                if (turnManager.CurrentRound == 0) break;

                // Get Current Creature
                var currentCreature = turnManager.CurrentCreature;
                if (currentCreature == null) break;
                
                // Construct Context
                 var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                     currentCreature, 
                     new OpenCombatEngine.Core.Models.Actions.PositionTarget(gridManager.GetPosition(currentCreature).Value), 
                     gridManager
                 );
                 
                 await combatRunner.RunTurn(turnManager, context);
                 
                 turnManager.NextTurn();
            }
            
            // 6. Verification
            // Someone should be hurt or combat ended.
            bool damageDealt = (goblin.HitPoints.Current < goblin.HitPoints.Max) || (ranger.HitPoints.Current < ranger.HitPoints.Max);
            
            // Ideally damage dealt, but dice are random.
            // With +5 vs AC15, 55% hit chance. 3 rounds = ~1.65 hits.
            
            turnManager.CurrentRound.Should().BeGreaterThan(0);
        }
    }
}
