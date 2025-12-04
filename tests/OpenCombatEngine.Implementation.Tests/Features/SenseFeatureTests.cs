using System.Collections.Generic;
using FluentAssertions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Features;
using OpenCombatEngine.Implementation;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Effects;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class SenseFeatureTests
    {
        [Fact]
        public void OnApplied_Should_Add_Sense_To_Creature()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var feature = new SenseFeature("Darkvision", "Darkvision", 60);
            feature.OnApplied(creature);

            creature.Senses.Should().ContainKey("Darkvision");
            creature.Senses["Darkvision"].Should().Be(60);
        }

        [Fact]
        public void OnApplied_Should_Update_Sense_If_Range_Is_Greater()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            creature.Senses["Darkvision"] = 30;

            var feature = new SenseFeature("Darkvision", "Darkvision", 60);
            feature.OnApplied(creature);

            creature.Senses["Darkvision"].Should().Be(60);
        }

        [Fact]
        public void OnApplied_Should_Not_Update_Sense_If_Range_Is_Smaller()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            creature.Senses["Darkvision"] = 60;

            var feature = new SenseFeature("Darkvision", "Darkvision", 30);
            feature.OnApplied(creature);

            creature.Senses["Darkvision"].Should().Be(60);
        }
    }
}
