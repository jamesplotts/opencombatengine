// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Text.Json;
using FluentAssertions;
using OpenCombatEngine.GrpcSidecar.Mapping;

namespace OpenCombatEngine.GrpcSidecar.Tests.Mapping;

/// <summary>
/// Tests for <see cref="CharacterSchema"/>, the hand-written JSON Schema
/// describing this engine's <c>CreatureState</c> wire shape.
/// </summary>
public class CharacterSchemaTests
{
    [Fact]
    public void Json_IsValidJson()
    {
        var act = () => JsonDocument.Parse(CharacterSchema.Json);

        act.Should().NotThrow();
    }

    [Fact]
    public void Json_DeclaresDraft2020_12()
    {
        using var doc = JsonDocument.Parse(CharacterSchema.Json);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void Json_RequiresCoreCreatureFields()
    {
        using var doc = JsonDocument.Parse(CharacterSchema.Json);

        var required = doc.RootElement.GetProperty("required")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        required.Should().Contain(new[] { "id", "name", "team", "abilityScores", "hitPoints" });
    }

    [Fact]
    public void Json_DescribesAbilityScoresAsAnObjectWithAllSixAbilities()
    {
        using var doc = JsonDocument.Parse(CharacterSchema.Json);

        var abilityScores = doc.RootElement.GetProperty("properties").GetProperty("abilityScores");
        var properties = abilityScores.GetProperty("properties");

        foreach (var ability in new[] { "strength", "dexterity", "constitution", "intelligence", "wisdom", "charisma" })
        {
            properties.TryGetProperty(ability, out _).Should().BeTrue($"abilityScores should describe '{ability}'");
        }
    }

    [Fact]
    public void SchemaVersion_MatchesActorMappingSchemaVersion()
    {
        CharacterSchema.SchemaVersion.Should().Be(ActorMapping.SchemaVersion);
    }
}
