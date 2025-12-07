using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Effects;
using OpenCombatEngine.Implementation.Dice;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class DurationTests
    {
        private readonly StandardCreature _creature;

        public DurationTests()
        {
            _creature = new StandardCreature(
                Guid.NewGuid().ToString(),
                "TestBot",
                new OpenCombatEngine.Implementation.Creatures.StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new OpenCombatEngine.Implementation.Creatures.StandardHitPoints(10),
                new OpenCombatEngine.Implementation.Items.StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );
        }

        [Fact]
        public void UntilEndOfTurn_Should_Expire_On_EndTurn()
        {
            // Arrange
            var effect = new StatBonusEffect("Shield", "AC +5", 1, StatType.ArmorClass, 5, DurationType.UntilEndOfTurn);
            _creature.Effects.AddEffect(effect);

            // Assert Active
            _creature.Effects.ActiveEffects.Should().Contain(e => e.Name == "Shield");

            // Act
            _creature.EndTurn();

            // Assert Expired
            _creature.Effects.ActiveEffects.Should().NotContain(e => e.Name == "Shield");
        }

        [Fact]
        public void UntilStartOfNextTurn_Should_Persist_Through_EndTurn_But_Expire_On_Next_StartTurn()
        {
            // Arrange
            // Note: Currently UntilStartOfNextTurn is treated as 1 Round by convention in StatBonusEffect constructor logic?
            // Actually, StatBonusEffect constructor takes int durationRounds.
            // If UntilStartOfNextTurn, we pass 1.
            var effect = new StatBonusEffect("Dodge", "AC +2", 1, StatType.ArmorClass, 2, DurationType.UntilStartOfNextTurn);
            _creature.Effects.AddEffect(effect);

            // Act 1: End Turn
            _creature.EndTurn();

            // Assert 1: Still Active
            _creature.Effects.ActiveEffects.Should().Contain(e => e.Name == "Dodge");

            // Act 2: Start Next Turn
            _creature.StartTurn();

            // Assert 2: Expired
            // Explanation: StartTurn -> Manager.Tick -> effect.OnTurnStart -> decrements Duration (1 -> 0) -> Manager Removes.
            _creature.Effects.ActiveEffects.Should().NotContain(e => e.Name == "Dodge");
        }

        [Fact]
        public void Round_Duration_Should_Decrement()
        {
            // Arrange
            var effect = new StatBonusEffect("Bless", "Hit +1d4", 2, StatType.AttackRoll, 2, DurationType.Round);
            _creature.Effects.AddEffect(effect);

            // Act 1: Start Turn 1
            _creature.StartTurn();
            // Decrements 2 -> 1. Still > 0.

            // Assert 1: Active
            _creature.Effects.ActiveEffects.Should().Contain(e => e.Name == "Bless");
            effect.DurationRounds.Should().Be(1);

            // Act 2: Start Turn 2
            _creature.StartTurn();
            // Decrements 1 -> 0.

            // Assert 2: Expired
            _creature.Effects.ActiveEffects.Should().NotContain(e => e.Name == "Bless");
        }
    }
}
