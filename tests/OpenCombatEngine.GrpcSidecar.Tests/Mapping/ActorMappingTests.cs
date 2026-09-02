// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using FluentAssertions;
using Layforge.Protocol.SystemEngine.V1;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.GrpcSidecar.Mapping;
using OpenCombatEngine.Implementation.Creatures;

namespace OpenCombatEngine.GrpcSidecar.Tests.Mapping;

/// <summary>
/// Tests for <see cref="ActorMapping"/>, which converts between the
/// System Engine gRPC contract's <see cref="Actor"/> message and a live
/// <see cref="StandardCreature"/>.
/// </summary>
public class ActorMappingTests
{
    private static CreatureState MakeState() => new(
        Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Name: "Kestrel",
        Team: "Player",
        AbilityScores: new AbilityScoresState(16, 12, 14, 10, 13, 8),
        HitPoints: new HitPointsState(24, 30, 0));

    [Fact]
    public void ToActor_StandardCreature_ProducesActorWithMatchingIdAndSchemaVersion()
    {
        var creature = new StandardCreature(MakeState());

        var actor = ActorMapping.ToActor(creature);

        actor.ActorId.Should().Be(creature.Id.ToString());
        actor.SchemaVersion.Should().Be(ActorMapping.SchemaVersion);
        actor.CharacterData.Fields["name"].StringValue.Should().Be("Kestrel");
    }

    [Fact]
    public void ToCreature_ActorFromToActor_RoundTripsCoreFields()
    {
        var original = new StandardCreature(MakeState());
        var actor = ActorMapping.ToActor(original);

        var result = ActorMapping.ToCreature(actor);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(original.Id);
        result.Value.Name.Should().Be(original.Name);
        result.Value.Team.Should().Be(original.Team);
        result.Value.HitPoints.Current.Should().Be(original.HitPoints.Current);
        result.Value.AbilityScores.Strength.Should().Be(original.AbilityScores.Strength);
    }

    [Fact]
    public void ToCreature_ActorWithMalformedCharacterData_ReturnsFailure()
    {
        var actor = new Actor
        {
            ActorId = Guid.NewGuid().ToString(),
            SchemaVersion = ActorMapping.SchemaVersion,
            // Empty Struct: none of CreatureState's required fields present.
        };

        var result = ActorMapping.ToCreature(actor);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
