using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Implementation.Classes;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Races;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ClassRaceTests
    {
        [Fact]
        public void Creature_Should_Have_Race_If_Provided()
        {
            var race = new RaceDefinition("Elf", 30, Size.Medium);
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Elf Hero",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller()),
                race: race
            );

            creature.Race.Should().Be(race);
        }

        [Fact]
        public void Racial_Features_Should_Be_Applied_On_Creation()
        {
            var feature = Substitute.For<IFeature>();
            var race = new RaceDefinition("Elf", 30, Size.Medium, racialFeatures: new[] { feature });

            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Elf Hero",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller()),
                race: race
            );

            feature.Received().OnApplied(creature);
        }

        [Fact]
        public void Class_Features_Should_Be_Applied_On_LevelUp()
        {
            var feature = Substitute.For<IFeature>();
            var features = new Dictionary<int, IEnumerable<IFeature>>
            {
                { 1, new[] { feature } }
            };
            var fighter = new ClassDefinition("Fighter", 10, features);

            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Hero",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            creature.LevelManager.LevelUp(fighter);

            feature.Received().OnApplied(creature);
        }
    }
}
