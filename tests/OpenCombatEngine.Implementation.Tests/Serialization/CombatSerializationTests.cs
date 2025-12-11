using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Combat;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Serialization;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Serialization
{
    public class CombatSerializationTests
    {
        private StandardTurnManager _turnManager;
        private StandardCombatManager _combatManager;
        private StandardDiceRoller _diceRoller;
        private CombatSerializer _serializer;

        public CombatSerializationTests()
        {
            _diceRoller = new StandardDiceRoller();
            _turnManager = new StandardTurnManager(_diceRoller);
            _combatManager = new StandardCombatManager(_turnManager);
            _serializer = new CombatSerializer();
        }

        private ICreature CreateCreature(string name, int hp, int dex, string team)
        {
             var abilities = new StandardAbilityScores(10, dex, 10, 10, 10, 10);
             var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                name,
                abilities,
                new StandardHitPoints(hp, hp, 0), 
                new OpenCombatEngine.Implementation.Items.StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller()) // Unused during combat, but required by constructor
            );
            creature.Team = team;
            return creature;
        }

        [Fact]
        public void Should_Save_And_Load_Combat_State()
        {
            // 1. Setup Initial Encounter
            var hero = CreateCreature("Hero", 20, 15, "Heroes");
            var goblin = CreateCreature("Goblin", 10, 12, "Monsters");

            _combatManager.StartEncounter(new[] { hero, goblin });

            // 2. Mutate State
            // Advance turn a few times
            _turnManager.NextTurn(); // Round 1, Turn 0
            _turnManager.NextTurn(); // Round 1, Turn 1
            _turnManager.NextTurn(); // Round 2, Turn 0

            // Deal damage
            hero.HitPoints.TakeDamage(5); // Current: 15
            goblin.HitPoints.TakeDamage(2); // Current: 8

            // 3. Serialize
            var json = _serializer.Serialize(_combatManager);

            // 4. Create New Manager and Deserialize
            var newTurnManager = new StandardTurnManager(new StandardDiceRoller());
            var newCombatManager = new StandardCombatManager(newTurnManager);
            
            _serializer.Deserialize(json, newCombatManager);

            // 5. Verify
            newCombatManager.Participants.Should().HaveCount(2);

            var restoredHero = newCombatManager.Participants.First(p => p.Name == "Hero");
            restoredHero.HitPoints.Current.Should().Be(15);
            restoredHero.Team.Should().Be("Heroes");

            var restoredGoblin = newCombatManager.Participants.First(p => p.Name == "Goblin");
            restoredGoblin.HitPoints.Current.Should().Be(8);

            // Verify Turn Manager
            newTurnManager.CurrentRound.Should().Be(2, "Should be round 2");
            // Turn order should be restored properly
            // Init: Hero (15 dex) > Goblin (12 dex)
            // Turns: Hero (0) -> Goblin (1) -> Hero (0)
            // State: Round 2, Current Index 0 (Hero)
            // BUT: The loop in test: StartEncounter runs NextTurn() to set index 0.
            // StartEncounter calls StartCombat -> NextTurn() => Index 0.
            // My NextTurn calls: 0 -> 1 -> 0 (Round 2).
            // So Current Turn should be Index 0 (Hero).
            
            // StandardTurnManager.StartCombat sets Round 1, Index -1, then NextTurn() => Index 0.
            // So:
            // Start: R1, T0 (Hero)
            // Next: R1, T1 (Goblin)
            // Next: R2, T0 (Hero)
            
            newTurnManager.CurrentCreature.Name.Should().Be("Hero");
        }
    }
}
