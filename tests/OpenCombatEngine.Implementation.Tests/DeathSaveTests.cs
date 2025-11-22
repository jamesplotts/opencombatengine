using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Creatures;
using System;
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
            var hp = new StandardHitPoints(10, 0, 0);
            
            var checkManager = new StandardCheckManager(abilityScores, diceRoller, Substitute.For<ICreature>());
            
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "name", abilityScores, hp, combatStats, checkManager);
            
            // Act
            creature.StartTurn();
            
            // Assert
            hp.DeathSaveSuccesses.Should().Be(1);
            hp.DeathSaveFailures.Should().Be(0);
        }

        [Fact]
        public void Should_Die_On_3_Failures()
        {
            // Arrange
            var hp = new StandardHitPoints(10, 0, 0);
            hp.RecordDeathSave(false);
            hp.RecordDeathSave(false);
            
            // Act
            hp.RecordDeathSave(false); // 3rd failure

            // Assert
            hp.IsDead.Should().BeTrue();
        }

        [Fact]
        public void Should_Reset_Saves_On_Stabilize()
        {
            // Arrange
            var hp = new StandardHitPoints(10, 0, 0);
            hp.RecordDeathSave(true);
            hp.RecordDeathSave(false);
            
            // Act
            hp.Stabilize();

            // Assert
            hp.IsStable.Should().BeTrue();
            hp.DeathSaveSuccesses.Should().Be(0);
            hp.DeathSaveFailures.Should().Be(0);
        }

        [Fact]
        public void Should_Reset_Saves_On_Heal()
        {
            // Arrange
            var hp = new StandardHitPoints(10, 0, 0);
            hp.RecordDeathSave(true);
            hp.RecordDeathSave(false);
            
            // Act
            hp.Heal(1);

            // Assert
            hp.Current.Should().Be(1);
            hp.IsStable.Should().BeFalse(); // Alive, not stable
            hp.DeathSaveSuccesses.Should().Be(0);
            hp.DeathSaveFailures.Should().Be(0);
        }

        [Fact]
        public void Creature_Should_Roll_Death_Save_Start_Of_Turn()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Normal)));

            var abilityScores = Substitute.For<IAbilityScores>();
            var hp = new StandardHitPoints(10, 0, 0);
            var combatStats = Substitute.For<ICombatStats>();
            
            var checkManager = new StandardCheckManager(abilityScores, diceRoller, Substitute.For<ICreature>());
            
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "name", abilityScores, hp, combatStats, checkManager);
            
            // Act
            creature.StartTurn();

            // Assert
            hp.DeathSaveSuccesses.Should().Be(1);
        }

        [Fact]
        public void Creature_Should_Not_Roll_Death_Save_If_Stable()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            
            var abilityScores = Substitute.For<IAbilityScores>();
            var hp = new StandardHitPoints(10, 0, 0);
            hp.Stabilize();
            
            var checkManager = new StandardCheckManager(abilityScores, diceRoller, Substitute.For<ICreature>());
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "name", abilityScores, hp, null, checkManager);
            
            // Act
            creature.StartTurn();

            // Assert
            diceRoller.DidNotReceive().Roll(Arg.Any<string>());
        }

        [Fact]
        public void Nat20_Should_Heal_1HP()
        {
            // Arrange
            var diceRoller = Substitute.For<IDiceRoller>();
            // Roll 20 (Nat 20)
            diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(20, "1d20", new List<int> { 20 }, 0, RollType.Normal)));
            
            var abilityScores = Substitute.For<IAbilityScores>();
            var hp = new StandardHitPoints(10, 0, 0);
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
            var hp = new StandardHitPoints(10, 0, 0);
            var checkManager = new StandardCheckManager(abilityScores, diceRoller, Substitute.For<ICreature>());
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "name", abilityScores, hp, null, checkManager);
            
            // Act
            creature.StartTurn();
            
            // Assert
            hp.DeathSaveFailures.Should().Be(2);
        }
    }
}
