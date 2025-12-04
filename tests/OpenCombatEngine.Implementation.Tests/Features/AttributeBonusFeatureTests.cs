using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Features;
using OpenCombatEngine.Implementation;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using FluentAssertions;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class AttributeBonusFeatureTests
    {
        [Fact]
        public void OnApplied_Should_Add_StatBonusEffect_To_Creature()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var feature = new AttributeBonusFeature("Fast Movement", "Speed", 10);
            feature.OnApplied(creature);

            // Verify effect was added
            // StandardEffectManager doesn't expose effects easily by name without iterating
            // But we can check if Speed is modified
            creature.CombatStats.Speed.Should().Be(40); // Base 30 + 10
        }

        [Fact]
        public void OnRemoved_Should_Remove_StatBonusEffect()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var feature = new AttributeBonusFeature("Fast Movement", "Speed", 10);
            feature.OnApplied(creature);
            creature.CombatStats.Speed.Should().Be(40);

            feature.OnRemoved(creature);
            creature.CombatStats.Speed.Should().Be(30);
        }
    }
}
