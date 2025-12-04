using FluentAssertions;
using OpenCombatEngine.Implementation.Features;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class FeatureFactoryTests
    {
        [Fact]
        public void CreateFeature_Should_Return_SenseFeature_For_Darkvision()
        {
            var feature = FeatureFactory.CreateFeature("Darkvision", "You can see in dim light within 60 feet of you as if it were bright light.");
            
            feature.Should().BeOfType<SenseFeature>();
            var sense = (SenseFeature)feature!;
            sense.Name.Should().Be("Darkvision");
            sense.SenseType.Should().Be("Darkvision");
            sense.Range.Should().Be(60);
        }

        [Fact]
        public void CreateFeature_Should_Return_TextFeature_For_Unknown_Feature()
        {
            var feature = FeatureFactory.CreateFeature("Keen Senses", "You have proficiency in the Perception skill.");
            
            feature.Should().BeOfType<TextFeature>();
            feature!.Name.Should().Be("Keen Senses");
        }

        [Fact]
        public void CreateFeature_Should_Parse_Range_From_Name()
        {
            var feature = FeatureFactory.CreateFeature("Darkvision (120 ft)", "Superior darkvision.");
            
            feature.Should().BeOfType<SenseFeature>();
            var sense = (SenseFeature)feature!;
            sense.Range.Should().Be(120);
        }

        [Fact]
        public void CreateFeature_Should_Return_AttributeBonusFeature_For_Speed()
        {
            var feature = FeatureFactory.CreateFeature("Speed +10", "Your speed increases by 10 feet.");
            
            feature.Should().BeOfType<AttributeBonusFeature>();
            var bonus = (AttributeBonusFeature)feature!;
            bonus.AttributeName.Should().Be("Speed");
            bonus.Bonus.Should().Be(10);
        }
    }
}
