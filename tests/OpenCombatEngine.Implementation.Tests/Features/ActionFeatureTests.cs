using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Features;
using OpenCombatEngine.Implementation;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using FluentAssertions;
using Xunit;
using System.Linq;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ActionFeatureTests
    {
        [Fact]
        public void OnApplied_Should_Add_Action()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var action = new TextAction("Test Action", "Description");
            var feature = new ActionFeature("Test Feature", action);

            feature.OnApplied(creature);

            creature.Actions.Should().Contain(action);
        }

        [Fact]
        public void OnRemoved_Should_Remove_Action()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var action = new TextAction("Test Action", "Description");
            var feature = new ActionFeature("Test Feature", action);

            feature.OnApplied(creature);
            creature.Actions.Should().Contain(action);

            feature.OnRemoved(creature);
            creature.Actions.Should().NotContain(action);
        }
    }
}
