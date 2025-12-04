using System.Linq;
using System.Text.Json;
using FluentAssertions;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Features;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class FeatureParsingTests
    {
        [Fact]
        public void ParseFeatures_Should_Parse_Single_Feature_Object()
        {
            var json = @"
            {
                ""name"": ""Darkvision"",
                ""entries"": [""You can see in dim light...""]
            }";
            var element = JsonDocument.Parse(json).RootElement;

            var features = FeatureParsingService.ParseFeatures(element).ToList();

            features.Should().HaveCount(1);
            var feature = features.First().Should().BeOfType<TextFeature>().Subject;
            feature.Name.Should().Be("Darkvision");
            feature.Description.Should().Contain("You can see in dim light");
        }

        [Fact]
        public void ParseFeatures_Should_Parse_Array_Of_Features()
        {
            var json = @"
            [
                { ""name"": ""Feature 1"", ""entries"": [""Desc 1""] },
                { ""name"": ""Feature 2"", ""entries"": [""Desc 2""] }
            ]";
            var element = JsonDocument.Parse(json).RootElement;

            var features = FeatureParsingService.ParseFeatures(element).ToList();

            features.Should().HaveCount(2);
            features[0].Name.Should().Be("Feature 1");
            features[1].Name.Should().Be("Feature 2");
        }

        [Fact]
        public void ParseFeatures_Should_Handle_Nested_Entries()
        {
            var json = @"
            {
                ""name"": ""Complex Feature"",
                ""entries"": [
                    ""Para 1"",
                    { ""type"": ""list"", ""entries"": [""Item 1"", ""Item 2""] }
                ]
            }";
            var element = JsonDocument.Parse(json).RootElement;

            var features = FeatureParsingService.ParseFeatures(element).ToList();

            features.Should().HaveCount(1);
            var feature = features.First() as TextFeature;
            feature.Description.Should().Contain("Para 1");
            feature.Description.Should().Contain("Item 1");
            feature.Description.Should().Contain("Item 2");
        }
    }
}
