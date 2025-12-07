using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Implementation.Content;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    [Collection("Integration")]
    public class JsonClassImporterTests
    {
        [Fact]
        public void Import_Should_Parse_Valid_Class_Json()
        {
            var json = @"
            {
                ""class"": [
                    {
                        ""name"": ""Fighter"",
                        ""source"": ""PHB"",
                        ""hd"": {
                            ""number"": 1,
                            ""faces"": 10
                        },
                        ""proficiency"": [
                            ""str"",
                            ""con""
                        ]
                    }
                ]
            }";

            var importer = new JsonClassImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var classes = result.Value.ToList();

            classes.Should().HaveCount(1);
            var fighter = classes.First();
            fighter.Name.Should().Be("Fighter");
            fighter.HitDie.Should().Be(10);
        }

        [Fact]
        public void Import_Should_Handle_Empty_Json()
        {
            var importer = new JsonClassImporter();
            var result = importer.Import("{}");
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        [Fact]
        public void Import_Should_Handle_Invalid_Json()
        {
            var importer = new JsonClassImporter();
            var result = importer.Import("invalid json");
            result.IsSuccess.Should().BeFalse();
        }
        [Fact]
        public void Import_Should_Parse_Class_Features()
        {
            var json = @"
            {
                ""class"": [
                    {
                        ""name"": ""Barbarian"",
                        ""hd"": { ""faces"": 12 },
                        ""classFeatures"": [
                            ""Rage"",
                            { ""classFeature"": ""Unarmored Defense|Barbarian||1"" }
                        ]
                    }
                ]
            }";

            var importer = new JsonClassImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var barbarian = result.Value.First();
            barbarian.FeaturesByLevel.Should().ContainKey(1);
            var features = barbarian.FeaturesByLevel[1];
            features.Should().Contain(f => f.Name == "Rage");
            features.Should().Contain(f => f.Name == "Unarmored Defense");
        }
    }
}
