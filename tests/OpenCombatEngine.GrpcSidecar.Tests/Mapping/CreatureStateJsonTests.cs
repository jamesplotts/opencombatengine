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
    public void Deserialize_ValidJsonWithGender_RoundTripsGender()
    {
        var original = MakeState() with { Gender = "Female" };
        var json = CreatureStateJson.Serialize(original);

        var result = CreatureStateJson.Deserialize(json);

        result.IsSuccess.Should().BeTrue();
        result.Value.Gender.Should().Be("Female");
    }

    [Fact]
    public void Deserialize_NoGenderInJson_GenderIsNull()
    {
        var result = CreatureStateJson.Deserialize(CreatureStateJson.Serialize(MakeState()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Gender.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ValidJsonWithRaceName_RoundTripsRaceName()
    {
        var original = MakeState() with { RaceName = "Dwarf" };
        var json = CreatureStateJson.Serialize(original);

        var result = CreatureStateJson.Deserialize(json);

        result.IsSuccess.Should().BeTrue();
        result.Value.RaceName.Should().Be("Dwarf");
    }

    [Fact]
    public void Deserialize_ValidJsonWithBackground_RoundTripsBackground()
    {
        var original = MakeState() with { Background = "Criminal" };
        var json = CreatureStateJson.Serialize(original);

        var result = CreatureStateJson.Deserialize(json);

        result.IsSuccess.Should().BeTrue();
        result.Value.Background.Should().Be("Criminal");
    }

    [Fact]
    public void Deserialize_NoBackgroundInJson_BackgroundIsNull()
    {
        var result = CreatureStateJson.Deserialize(CreatureStateJson.Serialize(MakeState()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Background.Should().BeNull();
    }

    [Fact]
    public void Deserialize_ValidJsonWithActionEconomy_RoundTripsActionEconomy()
    {
        var original = MakeState() with { ActionEconomy = new ActionEconomyState(true, false, true, false) };
        var json = CreatureStateJson.Serialize(original);

        var result = CreatureStateJson.Deserialize(json);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActionEconomy.Should().Be(new ActionEconomyState(true, false, true, false));
    }

    [Fact]
    public void Deserialize_ActionEconomyJsonMissingHasFreeObjectInteraction_DefaultsToTrue()
    {
        // Mirrors the Gender/RaceName/Background bug class directly: a save
        // written before HasFreeObjectInteraction existed must still restore
        // to "available" rather than silently binding to false. Unlike
        // those string fields (which bind to null when absent), this one is
        // a positional record parameter with its own default — the fixture
        // below hand-writes JSON as if from an older save, omitting the
        // field entirely, to prove the default actually takes effect
        // through JsonSerializer's constructor binding, not just in code
        // that never went through JSON at all.
        var json = """
            {"id":"11111111-1111-1111-1111-111111111111","name":"Kestrel","team":"Player",
             "abilityScores":{"strength":16,"dexterity":12,"constitution":14,"intelligence":10,"wisdom":13,"charisma":8},
             "hitPoints":{"current":24,"max":30,"temporary":0},
             "actionEconomy":{"hasAction":true,"hasBonusAction":true,"hasReaction":true}}
            """;

        var result = CreatureStateJson.Deserialize(json);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActionEconomy.Should().NotBeNull();
        result.Value.ActionEconomy!.HasFreeObjectInteraction.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_NoActionEconomyInJson_ActionEconomyIsNull()
    {
        var result = CreatureStateJson.Deserialize(CreatureStateJson.Serialize(MakeState()));

        result.IsSuccess.Should().BeTrue();
        result.Value.ActionEconomy.Should().BeNull();
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
