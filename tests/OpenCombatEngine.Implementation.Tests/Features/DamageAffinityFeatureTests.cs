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
    public class DamageAffinityFeatureTests
    {
        [Fact]
        public void OnApplied_Should_Add_Resistance()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var feature = new DamageAffinityFeature("Fire Resistance", DamageType.Fire, AffinityType.Resistance);
            feature.OnApplied(creature);

            creature.CombatStats.Resistances.Should().Contain(DamageType.Fire);
        }

        [Fact]
        public void OnApplied_Should_Add_Immunity()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var feature = new DamageAffinityFeature("Poison Immunity", DamageType.Poison, AffinityType.Immunity);
            feature.OnApplied(creature);

            creature.CombatStats.Immunities.Should().Contain(DamageType.Poison);
        }

        [Fact]
        public void OnApplied_Should_Add_Vulnerability()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var feature = new DamageAffinityFeature("Cold Vulnerability", DamageType.Cold, AffinityType.Vulnerability);
            feature.OnApplied(creature);

            creature.CombatStats.Vulnerabilities.Should().Contain(DamageType.Cold);
        }

        [Fact]
        public void OnRemoved_Should_Remove_Affinity()
        {
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var feature = new DamageAffinityFeature("Fire Resistance", DamageType.Fire, AffinityType.Resistance);
            feature.OnApplied(creature);
            creature.CombatStats.Resistances.Should().Contain(DamageType.Fire);

            feature.OnRemoved(creature);
            creature.CombatStats.Resistances.Should().NotContain(DamageType.Fire);
        }
    }
}
