// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using FluentAssertions;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.GrpcSidecar.Mapping;

namespace OpenCombatEngine.GrpcSidecar.Tests.Mapping;

/// <summary>
/// Tests for <see cref="CreatureStateJson"/>, which converts between
/// <see cref="CreatureState"/> and JSON strings.
/// </summary>
public class CreatureStateJsonTests
{
    private static CreatureState MakeState() => new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Name: "Kestrel",
        Team: "Player",
        AbilityScores: new AbilityScoresState(16, 12, 14, 10, 13, 8),
        HitPoints: new HitPointsState(24, 30, 0));

    [Fact]
    public void Serialize_ProducesJsonWithCamelCaseFieldNames()
    {
        var json = CreatureStateJson.Serialize(MakeState());

        // Not asserting on exact formatting, just that field names are
        // predictable enough for GetCharacterSchema's hand-written schema
        // (see CharacterSchema.cs) to actually describe what this emits.
        json.Should().Contain("\"name\"").And.Contain("Kestrel");
        json.Should().Contain("\"abilityScores\"");
        json.Should().Contain("\"hitPoints\"");
    }

    [Fact]
    public void Deserialize_ValidJson_RoundTripsAllFields()
    {
        var original = MakeState();
        var json = CreatureStateJson.Serialize(original);

        var result = CreatureStateJson.Deserialize(json);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(original); // CreatureState is a record: structural equality.
    }

    [Fact]
    public void Deserialize_MalformedJson_ReturnsFailureNotException()
    {
        var result = CreatureStateJson.Deserialize("{not valid json");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Deserialize_ValidJsonButWrongShape_ReturnsFailure()
    {
        // Valid JSON, but missing the required Name field a CreatureState
        // can't be constructed without.
        var result = CreatureStateJson.Deserialize("""{"id":"11111111-1111-1111-1111-111111111111"}""");

        result.IsSuccess.Should().BeFalse();
    }
}
