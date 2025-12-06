using System.Linq;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Races;
using OpenCombatEngine.Implementation.Builders;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Builders
{
    public class CharacterBuilderTests
    {
        [Fact]
        public void Build_Should_Create_Creature_With_Correct_Stats()
        {
            // Arrange
            var builder = new CharacterBuilder()
                .WithName("Hero")
                .WithAbilityScores(15, 14, 13, 12, 10, 8);

            // Act
            var creature = builder.Build();

            // Assert
            creature.Name.Should().Be("Hero");
            creature.AbilityScores.Strength.Should().Be(15);
            creature.AbilityScores.Dexterity.Should().Be(14);
            creature.AbilityScores.Constitution.Should().Be(13);
        }

        [Fact]
        public void Build_Should_Apply_Race_ASI()
        {
            // Arrange
            var race = Substitute.For<IRaceDefinition>();
            race.Name.Returns("Elf");
            race.AbilityScoreIncreases.Returns(new System.Collections.Generic.Dictionary<Ability, int>
            {
                { Ability.Dexterity, 2 }
            });

            var builder = new CharacterBuilder()
                .WithAbilityScores(10, 10, 10, 10, 10, 10)
                .WithRace(race);

            // Act
            var creature = builder.Build();

            // Assert
            creature.Race.Should().Be(race);
            creature.AbilityScores.Dexterity.Should().Be(12); // 10 + 2
        }

        [Fact]
        public void Build_Should_Apply_Class_Level_1_HP()
        {
            // Arrange
            var classDef = Substitute.For<IClassDefinition>();
            classDef.Name.Returns("Fighter");
            classDef.HitDie.Returns(10);
            classDef.FeaturesByLevel.Returns(new System.Collections.Generic.Dictionary<int, System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Features.IFeature>>());

            // Con 14 (+2)
            var builder = new CharacterBuilder()
                .WithAbilityScores(10, 10, 14, 10, 10, 10) 
                .WithClass(classDef);

            // Act
            var creature = builder.Build();

            // Assert
            // HP = 10 (Hit Die) + 2 (Con) = 12
            creature.HitPoints.Max.Should().Be(12);
            creature.HitPoints.Current.Should().Be(12);
            creature.LevelManager.TotalLevel.Should().Be(1);
        }
    }
}
