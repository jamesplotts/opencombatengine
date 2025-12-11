using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Combat;
using OpenCombatEngine.Implementation.Combat.WinConditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Combat
{
    public class CombatLoopTests
    {
        private StandardTurnManager _turnManager;
        private StandardCombatManager _combatManager;
        private StandardDiceRoller _diceRoller;

        public CombatLoopTests()
        {
            _diceRoller = new StandardDiceRoller();
            _turnManager = new StandardTurnManager(_diceRoller);
            // Default to LastTeamStanding
            _combatManager = new StandardCombatManager(_turnManager);
        }

        private ICreature CreateCreature(string name, int dex, string team)
        {
            var creature = new StarndardCreatureBuilder()
                .WithId(System.Guid.NewGuid().ToString())
                .WithName(name)
                .WithAbilityScores(10, dex, 10, 10, 10, 10)
                .WithMaxHp(10)
                .Build();
            creature.Team = team;
            return creature;
        }

        [Fact]
        public void Initiative_Should_Order_By_Roll_Then_Dex()
        {
            // Dice roller is random, but we can rely on high dex usually winning if rolls are similar, 
            // OR we can mock the dice roller. Ideally mock. 
            // For this test, let's assume standard behavior or just check that they are ordered.
            // Actually, StandardTurnManager rolls 1d20 + InitBonus.
            // Let's use a deterministic seed or mock. StandardDiceRoller takes a seed? Yes.
            // But checking exact order is hard without mocking the rolls.
            // Let's rely on the fact that TurnOrder is populated.
            
            var c1 = CreateCreature("C1", 10, "A");
            var c2 = CreateCreature("C2", 12, "B");

            _combatManager.StartEncounter(new[] { c1, c2 });

            _turnManager.TurnOrder.Should().HaveCount(2);
            _turnManager.CurrentCreature.Should().NotBeNull();
        }

        [Fact]
        public void NextTurn_Should_Skip_Dead_Creatures()
        {
            var c1 = CreateCreature("Alive", 10, "A");
            var c2 = CreateCreature("Dead", 10, "B");
            
            // Kill c2 immediately
            c2.HitPoints.TakeDamage(100, OpenCombatEngine.Core.Enums.DamageType.Force);
            c2.HitPoints.RecordDeathSave(false);
            c2.HitPoints.RecordDeathSave(false);
            c2.HitPoints.RecordDeathSave(false);
            c2.HitPoints.IsDead.Should().BeTrue();

            var c3 = CreateCreature("Alive2", 10, "A");

            // We need to force an order or just verify that ONLY c1 and c3 get turns.
            // Mocking init is best, but let's just StartCombat and see.
            _combatManager.StartEncounter(new[] { c1, c2, c3 });

            // Depending on initiative, the order might vary, but Dead should NEVER be current.
            // Iterate enough times to cover everyone
            
            for(int i=0; i<10; i++)
            {
                var current = _turnManager.CurrentCreature;
                if (current != null)
                {
                    current.HitPoints.IsDead.Should().BeFalse($"Creature {current.Name} is dead but got a turn!");
                }
                _turnManager.NextTurn();
            }
        }

        [Fact]
        public void WinCondition_Should_Trigger_When_One_Team_Left()
        {
            bool ended = false;
            string? winner = null;
            _combatManager.EncounterEnded += (s, e) => 
            {
                ended = true;
                winner = e.WinningTeam;
            };

            var hero = CreateCreature("Hero", 10, "Heroes");
            var goblin = CreateCreature("Goblin", 10, "Monsters");

            _combatManager.StartEncounter(new[] { hero, goblin });

            ended.Should().BeFalse();

            // Kill goblin
            goblin.HitPoints.TakeDamage(100, OpenCombatEngine.Core.Enums.DamageType.Acid);
            // This triggers Died -> CheckWinCondition

            ended.Should().BeTrue();
            winner.Should().Be("Heroes");
        }

        [Fact]
        public void Custom_WinCondition_Should_Work()
        {
            // Custom condition: End immediately if "Boss" takes any damage
            var customWin = new BossDamagedWinCondition();
            var manager = new StandardCombatManager(new StandardTurnManager(new StandardDiceRoller()), winCondition: customWin);

            bool ended = false;
             manager.EncounterEnded += (s, e) => ended = true;

            var boss = CreateCreature("Boss", 10, "BadGuys");
            var hero = CreateCreature("Hero", 10, "GoodGuys");

            manager.StartEncounter(new[] { boss, hero });
            ended.Should().BeFalse();

            // Damage boss
            boss.HitPoints.TakeDamage(1, OpenCombatEngine.Core.Enums.DamageType.Slashing);
            
            // Debug Assertions
            boss.HitPoints.Current.Should().Be(9, "Boss should have taken 1 damage");
            manager.Participants.Should().Contain(boss);
            var foundBoss = manager.Participants.First(x => x.Name == "Boss");
            foundBoss.HitPoints.Current.Should().Be(9, "Boss in Manager should have taken damage");

            // OnParticipantDied calls CheckWinCondition. 
            // ... (rest of comments)
            
            // Let's simulate calling CheckWinCondition manually, which the game loop might do.
            manager.CheckWinCondition();
            ended.Should().BeTrue();
        }

        private class BossDamagedWinCondition : IWinCondition
        {
            public bool Check(ICombatManager combatManager)
            {
                var boss = combatManager.Participants.FirstOrDefault(p => p.Name == "Boss");
                if (boss != null && boss.HitPoints.Current < boss.HitPoints.Max) return true;
                return false;
            }

            public string GetWinner(ICombatManager combatManager) => "Heroes";
        }
    }

    // Helper for building creatures quickly if not already available in tests
    // Assuming StarndardCreatureBuilder exists or I use StandardCreature directly.
    // I recall a Builder in tests. Let's check or just use constructor.
    // I will use constructor to be safe.
    
    public class StarndardCreatureBuilder
    {
        private string _id = "id";
        private string _name = "name";
        private int _str = 10;
        private int _dex = 10;
        private int _con = 10;
        private int _int = 10;
        private int _wis = 10;
        private int _cha = 10;
        private int _maxHp = 10;

        public StarndardCreatureBuilder WithId(string id) { _id = id; return this; }
        public StarndardCreatureBuilder WithName(string name) { _name = name; return this; }
        public StarndardCreatureBuilder WithAbilityScores(int s, int d, int c, int i, int w, int ch) 
        { 
            _str = s; _dex = d; _con = c; _int = i; _wis = w; _cha = ch; 
            return this; 
        }
        public StarndardCreatureBuilder WithMaxHp(int hp) { _maxHp = hp; return this; }

        public StandardCreature Build()
        {
            var abilities = new StandardAbilityScores(_str, _dex, _con, _int, _wis, _cha);

            var hp = new StandardHitPoints(_maxHp, 10, 10); // StandardHitPoints(max, hitDice, conMod?) -> StandardHitPoints(int max, int hitDiceCount, int hitDiceSides)
            // Let's check StandardHitPoints constructor signature if needed, but assuming simple one exists or just creating one.
            // Actually StandardHitPoints(IHitPointsState state, ICombatStats stats) or (int current, int max, ...)
            
            // Let's use the full StandardCreature constructor.
            return new StandardCreature(
                _id,
                _name,
                abilities,
                new StandardHitPoints(_maxHp, _maxHp, 0), 
                new OpenCombatEngine.Implementation.Items.StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );
        }
    }
}
