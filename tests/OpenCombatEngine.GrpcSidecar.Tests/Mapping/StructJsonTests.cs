// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using FluentAssertions;
using OpenCombatEngine.GrpcSidecar.Mapping;
using ProtoStruct = Google.Protobuf.WellKnownTypes.Struct;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;

namespace OpenCombatEngine.GrpcSidecar.Tests.Mapping;

/// <summary>
/// Tests for <see cref="StructJson"/>, which converts between
/// <see cref="Struct"/> (the wire representation of
/// Actor.character_data) and plain JSON strings.
/// </summary>
public class StructJsonTests
{
    [Fact]
    public void ToJson_SimpleStruct_ProducesPlainJsonObject()
    {
        var value = new ProtoStruct();
        value.Fields["name"] = ProtoValue.ForString("Kestrel");
        value.Fields["level"] = ProtoValue.ForNumber(3);
        value.Fields["is_alive"] = ProtoValue.ForBool(true);

        var json = StructJson.ToJson(value);

        // Plain JSON, not a protobuf-specific envelope — this is the
        // assumption the rest of the sidecar's mapping layer depends on.
        json.Should().Contain("\"name\"").And.Contain("Kestrel");
        json.Should().Contain("\"level\"").And.Contain("3");
        json.Should().Contain("\"is_alive\"").And.Contain("true");
    }

    [Fact]
    public void FromJson_PlainJsonObject_ProducesEquivalentStruct()
    {
        const string json = """{"name":"Kestrel","level":3,"is_alive":true}""";

        var value = StructJson.FromJson(json);

        value.Fields["name"].StringValue.Should().Be("Kestrel");
        value.Fields["level"].NumberValue.Should().Be(3);
        value.Fields["is_alive"].BoolValue.Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_NestedObjectAndArray_PreservesShape()
    {
        const string json = """
            {
                "abilityScores": {"strength": 16, "dexterity": 12},
                "tags": ["Undead", "Role:Artillery"]
            }
            """;

        var roundTripped = StructJson.ToJson(StructJson.FromJson(json));
        var reparsed = StructJson.FromJson(roundTripped);

        reparsed.Fields["abilityScores"].StructValue.Fields["strength"].NumberValue.Should().Be(16);
        reparsed.Fields["tags"].ListValue.Values.Should().HaveCount(2);
        reparsed.Fields["tags"].ListValue.Values[0].StringValue.Should().Be("Undead");
    }

    [Fact]
    public void FromJson_MalformedJson_ThrowsInvalidJsonException()
    {
        // Documented behavior, not swallowed: callers (the gRPC service
        // methods) are expected to catch this and translate it into a
        // response's own success=false/error field, per this project's
        // "no exceptions from public APIs" convention applied at the
        // gRPC-method boundary rather than at this low-level helper.
        var act = () => StructJson.FromJson("{not valid json");

        act.Should().Throw<Google.Protobuf.InvalidJsonException>();
    }
}
