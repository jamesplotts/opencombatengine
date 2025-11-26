using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class StandardTurnManagerTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly StandardTurnManager _turnManager;

        public StandardTurnManagerTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            _turnManager = new StandardTurnManager(_diceRoller);
        }

        [Fact]
        public void StartCombat_Should_Sort_By_Initiative_Total()
        {
            // Arrange
            var c1 = CreateCreature("Fast", dex: 10, initBonus: 5);
            var c2 = CreateCreature("Slow", dex: 10, initBonus: 0);

            // Mock Rolls: c1 rolls 15 (total 20), c2 rolls 10 (total 10)
            _diceRoller.Roll("1d20+5").Returns(Result<DiceRollResult>.Success(new DiceRollResult(20, "1d20+5", new List<int> { 15 }, 5, RollType.Normal)));
            _diceRoller.Roll("1d20+0").Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20+0", new List<int> { 10 }, 0, RollType.Normal)));

            // Act
            _turnManager.StartCombat(new[] { c2, c1 }); // Pass in wrong order

            // Assert
            _turnManager.TurnOrder.Should().ContainInOrder(c1, c2);
            _turnManager.CurrentCreature.Should().Be(c1);
            _turnManager.CurrentRound.Should().Be(1);
        }

        [Fact]
        public void StartCombat_Should_Break_Ties_With_Dexterity()
        {
            // Arrange
            var c1 = CreateCreature("HighDex", dex: 18, initBonus: 0);
            var c2 = CreateCreature("LowDex", dex: 10, initBonus: 0);

            // Mock Rolls: Both roll 10 (total 10)
            _diceRoller.Roll("1d20+0").Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20+0", new List<int> { 10 }, 0, RollType.Normal)));

            // Act
            _turnManager.StartCombat(new[] { c2, c1 });

            // Assert
            _turnManager.TurnOrder.Should().ContainInOrder(c1, c2); // High Dex first
        }

        [Fact]
        public void NextTurn_Should_Cycle_And_Increment_Round()
        {
            // Arrange
            var c1 = CreateCreature("A", 10, 0);
            var c2 = CreateCreature("B", 10, 0);
            
            // Mock rolls to ensure order A, B
            _diceRoller.Roll(Arg.Any<string>()).Returns(x => 
            {
                string notation = x.Arg<string>();
                // This is a bit hacky for mocking, assuming we call in order or can distinguish.
                // Better to just force the order via the mock returns if we know the call order.
                // But StartCombat iterates the input list.
                return Result<DiceRollResult>.Success(new DiceRollResult(10, notation, new List<int> { 10 }, 0, RollType.Normal));
            });
            
            // We need to ensure A rolls higher than B or has higher Dex.
            // Let's just make C1 have higher Dex.
            var fast = CreateCreature("Fast", 18, 0);
            var slow = CreateCreature("Slow", 10, 0);
            
            _turnManager.StartCombat(new[] { slow, fast }); // Order should be Fast, Slow

            // Act & Assert
            _turnManager.CurrentRound.Should().Be(1);
            _turnManager.CurrentCreature.Should().Be(fast);

            _turnManager.NextTurn();
            _turnManager.CurrentRound.Should().Be(1);
            _turnManager.CurrentCreature.Should().Be(slow);

            _turnManager.NextTurn();
            _turnManager.CurrentRound.Should().Be(2); // Round incremented
            _turnManager.CurrentCreature.Should().Be(fast); // Back to start
        }

        private ICreature CreateCreature(string name, int dex, int initBonus)
        {
            var creature = Substitute.For<ICreature>();
            creature.Id.Returns(Guid.NewGuid());
            creature.Name.Returns(name);
            creature.Team.Returns("Neutral");

            var stats = Substitute.For<ICombatStats>();
            stats.InitiativeBonus.Returns(initBonus);
            creature.CombatStats.Returns(stats);

            var abilities = Substitute.For<IAbilityScores>();
            abilities.Dexterity.Returns(dex);
            abilities.GetModifier(Ability.Dexterity).Returns((dex - 10) / 2);
            creature.AbilityScores.Returns(abilities);

            return creature;
        }
    }
}
