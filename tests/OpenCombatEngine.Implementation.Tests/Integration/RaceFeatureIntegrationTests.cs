using System.Linq;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Content;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Features;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Integration
{
    [Collection("Integration")]
    public class RaceFeatureIntegrationTests
    {
        [Fact]
        public void Import_Should_Create_SpellcastingFeature_For_Race()
        {
            // Arrange
            var repo = Substitute.For<ISpellRepository>();
            var spell = Substitute.For<ISpell>();
            spell.Name.Returns("Dancing Lights");
            repo.GetSpell("Dancing Lights").Returns(Result<ISpell>.Success(spell));
            FeatureFactory.SetSpellRepository(repo);

            var json = @"{
                ""race"": [
                    {
                        ""name"": ""High Elf"",
                        ""entries"": [
                            {
                                ""name"": ""Cantrip"",
                                ""entries"": [""You know the Dancing Lights cantrip.""]
                            }
                        ]
                    }
                ]
            }";
            var importer = new JsonRaceImporter();

            // Act
            var result = importer.Import(json);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var race = result.Value.First();
            race.Name.Should().Be("High Elf");
            
            race.RacialFeatures.Should().Contain(f => f is SpellcastingFeature);
            var feature = (SpellcastingFeature)race.RacialFeatures.First(f => f is SpellcastingFeature);
            feature.Spells.Should().Contain(spell);
        }

        [Fact]
        public void Import_Should_Create_ProficiencyFeature_For_Race()
        {
            // Arrange
            var json = @"{
                ""race"": [
                    {
                        ""name"": ""Wood Elf"",
                        ""entries"": [
                            {
                                ""name"": ""Mask of the Wild"",
                                ""entries"": [""Proficiency in Stealth skill.""]
                            }
                        ]
                    }
                ]
            }";
            var importer = new JsonRaceImporter();

            // Act
            var result = importer.Import(json);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var race = result.Value.First();
            
            race.RacialFeatures.Should().Contain(f => f is ProficiencyFeature);
            var feature = (ProficiencyFeature)race.RacialFeatures.First(f => f is ProficiencyFeature);
            feature.SkillName.Should().Be("Stealth");
        }
    }
}
