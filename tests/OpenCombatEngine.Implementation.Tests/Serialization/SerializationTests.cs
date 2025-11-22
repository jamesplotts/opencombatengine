using System.Text.Json;
using FluentAssertions;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Serialization
{
    public class SerializationTests
    {
        [Fact]
        public void Creature_Should_Serialize_And_Deserialize_Correctly()
        {
            // Arrange
            var scores = new StandardAbilityScores(18, 14, 12, 10, 8, 16);
            var hp = new StandardHitPoints(50, 45, 5);
            var original = new StandardCreature(Guid.NewGuid().ToString(), "Test Hero", scores, hp);

            // Act - Get State
            var state = original.GetState();

            // Act - Serialize to JSON
            var json = JsonSerializer.Serialize(state);

            // Act - Deserialize from JSON
            var deserializedState = JsonSerializer.Deserialize<CreatureState>(json);

            // Act - Reconstruct Creature
            var reconstructed = new StandardCreature(deserializedState!);

            // Assert
            reconstructed.Id.Should().Be(original.Id);
            reconstructed.Name.Should().Be(original.Name);
            
            reconstructed.AbilityScores.Strength.Should().Be(original.AbilityScores.Strength);
            reconstructed.AbilityScores.Dexterity.Should().Be(original.AbilityScores.Dexterity);
            
            reconstructed.HitPoints.Max.Should().Be(original.HitPoints.Max);
            reconstructed.HitPoints.Current.Should().Be(original.HitPoints.Current);
            reconstructed.HitPoints.Temporary.Should().Be(original.HitPoints.Temporary);
        }

        [Fact]
        public void AbilityScores_Should_RoundTrip_State()
        {
            // Arrange
            var original = new StandardAbilityScores(15, 14, 13, 12, 11, 10);

            // Act
            var state = original.GetState();
            var reconstructed = new StandardAbilityScores(state);

            // Assert
            reconstructed.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void HitPoints_Should_RoundTrip_State()
        {
            // Arrange
            var original = new StandardHitPoints(100, 50, 10);

            // Act
            var state = original.GetState();
            var reconstructed = new StandardHitPoints(state);

            // Assert
            reconstructed.Max.Should().Be(original.Max);
            reconstructed.Current.Should().Be(original.Current);
            reconstructed.Temporary.Should().Be(original.Temporary);
        }
    }
}
