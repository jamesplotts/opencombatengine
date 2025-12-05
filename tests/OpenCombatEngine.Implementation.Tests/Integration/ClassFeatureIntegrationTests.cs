using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Features;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Integration
{
    public class ClassFeatureIntegrationTests
    {
        [Fact]
        public void Import_Should_Create_ProficiencyFeatures_For_Class()
        {
            // Arrange
            var json = @"{
                ""class"": [
                    {
                        ""name"": ""Rogue"",
                        ""hd"": { ""number"": 1, ""faces"": 8 },
                        ""proficiency"": [""Light Armor"", ""Simple Weapons"", ""Thieves' Tools"", ""Dexterity"", ""Intelligence"", ""Stealth"", ""Perception""]
                    }
                ]
            }";
            var importer = new JsonClassImporter();

            // Act
            var result = importer.Import(json);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var rogue = result.Value.First();
            rogue.Name.Should().Be("Rogue");
            
            rogue.FeaturesByLevel.Should().ContainKey(1);
            var features = rogue.FeaturesByLevel[1].ToList();
            
            features.Should().Contain(f => f is ProficiencyFeature && ((ProficiencyFeature)f).SkillName == "Stealth");
            features.Should().Contain(f => f is ProficiencyFeature && ((ProficiencyFeature)f).SkillName == "Light Armor");
            
            // Verify count
            features.Count.Should().Be(7);
        }
    }
}
