using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
using System;
using System.Collections.Generic;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class RestTests
    {
        private readonly IDiceRoller _diceRoller;

        public RestTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
        }

        [Fact]
        public void ShortRest_Should_Heal_Using_HitDice()
        {
            // Arrange
            var scores = new StandardAbilityScores(10, 10, 14, 10, 10, 10); // Con +2
            var hp = new StandardHitPoints(20, 10, 0, hitDice: "1d8", hitDiceTotal: 2, diceRoller: _diceRoller);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Test", scores, hp, new StandardInventory(), new StandardTurnManager(_diceRoller));

            // Mock Dice Roll: 5
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(5, "1d8", new List<int> { 5 }, 0, RollType.Normal)));

            // Act
            creature.Rest(RestType.ShortRest, hitDiceToSpend: 1);

            // Assert
            // Healed: 5 (roll) + 2 (Con) = 7. Current: 10 + 7 = 17.
            creature.HitPoints.Current.Should().Be(17);
            creature.HitPoints.HitDiceRemaining.Should().Be(1);
        }

        [Fact]
        public void LongRest_Should_Heal_To_Max_And_Recover_HitDice()
        {
            // Arrange
            var hp = new StandardHitPoints(20, 5, 0, hitDice: "1d8", hitDiceTotal: 4, diceRoller: _diceRoller);
            // Use Hit Dice first to reduce remaining
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(new DiceRollResult(5, "1d8", new List<int> { 5 }, 0, RollType.Normal)));
            hp.UseHitDice(3); // Remaining: 1
            
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Test", new StandardAbilityScores(), hp, new StandardInventory(), new StandardTurnManager(_diceRoller));

            // Act
            creature.Rest(RestType.LongRest);

            // Assert
            creature.HitPoints.Current.Should().Be(20);
            // Recover half max hit dice: 4 / 2 = 2.
            // Remaining was 1. 1 + 2 = 3.
            creature.HitPoints.HitDiceRemaining.Should().Be(3);
        }
    }
}
