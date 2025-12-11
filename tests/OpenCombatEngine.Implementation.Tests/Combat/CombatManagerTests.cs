using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Combat;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Combat
{
    public class CombatManagerTests
    {
        [Fact]
        public void StartEncounter_Should_Initialize_TurnManager()
        {
            var turnManager = Substitute.For<ITurnManager>();
            var manager = new StandardCombatManager(turnManager);
            
            var participants = new List<ICreature>
            {
               new StandardCreature(Guid.NewGuid().ToString(), "P1", new StandardAbilityScores(10,10,10,10,10,10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller())) { Team = "A" },
               new StandardCreature(Guid.NewGuid().ToString(), "E1", new StandardAbilityScores(10,10,10,10,10,10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller())) { Team = "B" }
            };

            bool started = false;
            manager.EncounterStarted += (s, e) => started = true;

            manager.StartEncounter(participants);

            turnManager.Received(1).StartCombat(Arg.Is<IEnumerable<ICreature>>(x => x.Count() == 2));
            started.Should().BeTrue();
        }

        [Fact]
        public void Death_Should_Trigger_WinCondition_Check()
        {
            var turnManager = Substitute.For<ITurnManager>();
            var manager = new StandardCombatManager(turnManager);
            
            var p1 = new StandardCreature(Guid.NewGuid().ToString(), "P1", new StandardAbilityScores(10,10,10,10,10,10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller())) { Team = "A" };
            var e1 = new StandardCreature(Guid.NewGuid().ToString(), "E1", new StandardAbilityScores(10,10,10,10,10,10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller())) { Team = "B" };
            
            var participants = new List<ICreature> { p1, e1 };

            string? winner = null;
            manager.EncounterEnded += (s, e) => winner = e.WinningTeam;

            manager.StartEncounter(participants);

            // Kill E1
            e1.HitPoints.TakeDamage(20, OpenCombatEngine.Core.Enums.DamageType.Slashing); // Kill

            winner.Should().Be("A");
            turnManager.Received(1).EndCombat();
        }
        
        [Fact]
        public void CheckWinCondition_Should_Handle_Draw_Or_Empty()
        {
             var turnManager = Substitute.For<ITurnManager>();
             var manager = new StandardCombatManager(turnManager);
             var p1 = new StandardCreature(Guid.NewGuid().ToString(), "P1", new StandardAbilityScores(10,10,10,10,10,10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller())) { Team = "A" };
             
             // Starting with one team immediately wins?
             // Should check logic.
             // If activeTeams <= 1.
             
             string? winner = null;
             manager.EncounterEnded += (s, e) => winner = e.WinningTeam;
             
             manager.StartEncounter(new[] { p1 });
             
             // StartEncounter doesn't auto-check immediately unless we call it.
             // But let's say we trigger a check manually or event.
             manager.CheckWinCondition();
             
             winner.Should().Be("A");
        }
    }
}
