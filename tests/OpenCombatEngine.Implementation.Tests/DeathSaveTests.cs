using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;

using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Creatures;
using System.Collections.Generic;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class DeathSaveTests
    {
        [Fact]
        public void StartTurn_Should_Roll_DeathSave_When_Unconscious()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            // Roll 10 (Success)
            diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Normal)));
            
            var abilityScores = Substitute.For<IAbilityScores>();
            var combatStats = new StandardCombatStats();
            var hp = new StandardHitPoints(10, current: 0);
            
            // Create creature with mocked CheckManager (via mocked DiceRoller)
            // Actually, we can just inject a CheckManager that uses our mocked DiceRoller.
            // But we need to construct StandardCheckManager with the creature... which we are constructing.
            // Circular dependency for testing?
            // StandardCheckManager takes ICreature.
            // We can pass null for creature if we know RollDeathSave doesn't use it?
            // RollDeathSave doesn't use _creature.
            // So we can create the manager first.
            
            var checkManager = new StandardCheckManager(abilityScores, diceRoller, Substitute.For<ICreature>());
            
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "name", abilityScores, hp, combatStats, checkManager);
            
            // Act
            creature.StartTurn();
            
            // Assert
            hp.DeathSaveSuccesses.Should().Be(1);
            hp.DeathSaveFailures.Should().Be(0);
        }

        [Fact]
        public void Nat20_Should_Heal_1HP()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            // Roll 20 (Nat 20)
            diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(20, "1d20", new List<int> { 20 }, 0, RollType.Normal)));
            
            var abilityScores = Substitute.For<IAbilityScores>();
            var hp = new StandardHitPoints(10, current: 0);
            var checkManager = new StandardCheckManager(abilityScores, diceRoller, Substitute.For<ICreature>());
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "name", abilityScores, hp, null, checkManager);
            
            // Act
            creature.StartTurn();
            
            // Assert
            hp.Current.Should().Be(1);
            hp.IsStable.Should().BeFalse(); // Healed means conscious, not just stable
            hp.DeathSaveSuccesses.Should().Be(0); // Reset on heal
        }

        [Fact]
        public void Nat1_Should_Add_2_Failures()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            // Roll 1 (Nat 1)
            diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(1, "1d20", new List<int> { 1 }, 0, RollType.Normal)));
            
            var abilityScores = Substitute.For<IAbilityScores>();
            var hp = new StandardHitPoints(10, current: 0);
            var checkManager = new StandardCheckManager(abilityScores, diceRoller, Substitute.For<ICreature>());
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "name", abilityScores, hp, null, checkManager);
            
            // Act
            creature.StartTurn();
            
            // Assert
            hp.DeathSaveFailures.Should().Be(2);
        }
    }
}
