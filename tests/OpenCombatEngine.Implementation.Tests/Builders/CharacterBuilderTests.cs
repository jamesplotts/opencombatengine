using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Races;
using OpenCombatEngine.Implementation.Builders;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Builders
{
    public class CharacterBuilderTests
    {
        [Fact]
        public void Build_Should_Create_Creature_With_Name_And_Scores()
        {
            var builder = new CharacterBuilder();
            var creature = builder
                .WithName("Test Hero")
                .WithAbilityScores(15, 14, 13, 12, 10, 8)
                .Build();

            creature.Name.Should().Be("Test Hero");
            creature.AbilityScores.Strength.Should().Be(15);
        }

        [Fact]
        public void Build_Should_Add_And_Equip_Items()
        {
            var item = Substitute.For<OpenCombatEngine.Core.Interfaces.Items.IWeapon>();
            item.Name.Returns("Magic Sword");
            item.Id.Returns(Guid.NewGuid());
            // item.Slots is not available, Builder logic handles it by type.
            
            var builder = new CharacterBuilder();
            var creature = builder
                .WithEquipment(item)
                .Build();

            creature.Inventory.Items.Should().Contain(item);
            creature.Equipment.MainHand.Should().Be(item);
        }

        [Fact]
        public void Build_Should_Apply_Racial_Ability_Bonuses()
        {
            var race = Substitute.For<IRaceDefinition>();
            race.AbilityScoreIncreases.Returns(new Dictionary<Ability, int>
            {
                { Ability.Strength, 2 },
                { Ability.Charisma, 1 }
            });
            race.RacialFeatures.Returns(new List<IFeature>());

            var builder = new CharacterBuilder();
            var creature = builder
                .WithAbilityScores(10, 10, 10, 10, 10, 10)
                .WithRace(race)
                .Build();

            // Base 10 + 2 Str = 12
            // Base 10 + 1 Cha = 11
            creature.AbilityScores.Strength.Should().Be(12);
            creature.AbilityScores.Charisma.Should().Be(11);
            creature.AbilityScores.Dexterity.Should().Be(10);
        }

        [Fact]
        public void Build_Should_Set_HP_For_Level_1_Class()
        {
            var charClass = Substitute.For<IClassDefinition>();
            charClass.HitDie.Returns(10); // d10
            charClass.FeaturesByLevel.Returns(new Dictionary<int, IEnumerable<IFeature>>());
            
            // Con 14 (+2 mod).
            // Level 1 HP = Max(d10) + ConMod = 10 + 2 = 12.
            
            var builder = new CharacterBuilder();
            var creature = builder
                .WithAbilityScores(10, 10, 14, 10, 10, 10) // Con 14
                .WithClass(charClass)
                .Build();

            creature.HitPoints.Max.Should().Be(12);
            creature.HitPoints.Current.Should().Be(12);
        }

        /*
        [Fact]
        public void Build_Should_Apply_Class_Proficiencies()
        {
            // Proficiencies are not directly on IClassDefinition, likely handled via Features.
            // Skipping for now until feature application is verified explicitly.
        }
        */
    }
}
