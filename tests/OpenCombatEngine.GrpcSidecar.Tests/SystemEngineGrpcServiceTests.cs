// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Layforge.Protocol.SystemEngine.V1;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.GrpcSidecar;
using OpenCombatEngine.GrpcSidecar.Mapping;
using OpenCombatEngine.Implementation.Creatures;
using ProtoValue = Google.Protobuf.WellKnownTypes.Value;

namespace OpenCombatEngine.GrpcSidecar.Tests;

/// <summary>
/// Tests for <see cref="SystemEngineGrpcService"/>. The service never reads
/// from <see cref="ServerCallContext"/>, so tests pass <c>null!</c> for it,
/// matching how the ASP.NET Core gRPC host actually invokes these methods
/// (context is only needed for cancellation/peer/metadata access, none of
/// which this service uses).
/// </summary>
public class SystemEngineGrpcServiceTests
{
    private readonly SystemEngineGrpcService _service = new();

    private static CreatureState MakeState(int currentHp = 24, int maxHp = 30) => new(
        Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
        Name: "Kestrel",
        Team: "Player",
        AbilityScores: new AbilityScoresState(16, 12, 14, 10, 13, 8),
        HitPoints: new HitPointsState(currentHp, maxHp, 0));

    private static Actor MakeActor(int currentHp = 24, int maxHp = 30) =>
        ActorMapping.ToActor(new StandardCreature(MakeState(currentHp, maxHp)));

    [Fact]
    public async Task GetCharacterSchema_ReturnsCharacterSchemaJson()
    {
        var response = await _service.GetCharacterSchema(new GetCharacterSchemaRequest(), null!);

        response.SchemaVersion.Should().Be(CharacterSchema.SchemaVersion);
        response.JsonSchema.Should().Be(CharacterSchema.Json);
    }

    [Fact]
    public async Task ToJson_ValidActor_RoundTripsThroughFromJson()
    {
        var actor = MakeActor();

        var toJsonResponse = await _service.ToJson(new ToJsonRequest { Actor = actor }, null!);
        var fromJsonResponse = await _service.FromJson(new FromJsonRequest { Json = toJsonResponse.Json }, null!);

        fromJsonResponse.Actor.ActorId.Should().Be(actor.ActorId);
        fromJsonResponse.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ToJson_ActorWithMalformedCharacterData_ThrowsInvalidArgumentRpcException()
    {
        var badActor = new Actor { ActorId = "x", CharacterData = new Struct(), SchemaVersion = ActorMapping.SchemaVersion };

        var act = () => _service.ToJson(new ToJsonRequest { Actor = badActor }, null!);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task FromJson_MalformedJson_ReturnsErrorWarningNotException()
    {
        var response = await _service.FromJson(new FromJsonRequest { Json = "{not valid" }, null!);

        response.Warnings.Should().ContainSingle(w => w.Severity == "error");
    }

    [Fact]
    public async Task GetCharacterStatus_PositiveHp_ReturnsActive()
    {
        var response = await _service.GetCharacterStatus(
            new GetCharacterStatusRequest { Actor = MakeActor(currentHp: 24) }, null!);

        response.Status.Should().Be(CharacterStatus.Active);
    }

    [Fact]
    public async Task GetCharacterStatus_ZeroHp_ReturnsDying()
    {
        var response = await _service.GetCharacterStatus(
            new GetCharacterStatusRequest { Actor = MakeActor(currentHp: 0) }, null!);

        response.Status.Should().Be(CharacterStatus.Dying);
    }

    [Fact]
    public async Task ApplyEffect_Damage_ReducesHitPointsInReturnedActor()
    {
        var request = new ApplyEffectRequest
        {
            RequestId = "req-1",
            CampaignId = "campaign-1",
            Actor = MakeActor(currentHp: 24, maxHp: 30),
            Effect = new Struct
            {
                Fields =
                {
                    ["effectType"] = ProtoValue.ForString("damage"),
                    ["amount"] = ProtoValue.ForNumber(5),
                    ["damageType"] = ProtoValue.ForString("Fire"),
                },
            },
        };

        var response = await _service.ApplyEffect(request, null!);

        response.Success.Should().BeTrue();
        response.Actor.CharacterData.Fields["hitPoints"].StructValue.Fields["current"].NumberValue.Should().Be(19);
    }

    [Fact]
    public async Task ApplyEffect_Heal_IncreasesHitPointsInReturnedActor()
    {
        var request = new ApplyEffectRequest
        {
            RequestId = "req-2",
            CampaignId = "campaign-1",
            Actor = MakeActor(currentHp: 10, maxHp: 30),
            Effect = new Struct
            {
                Fields = { ["effectType"] = ProtoValue.ForString("heal"), ["amount"] = ProtoValue.ForNumber(6) },
            },
        };

        var response = await _service.ApplyEffect(request, null!);

        response.Success.Should().BeTrue();
        response.Actor.CharacterData.Fields["hitPoints"].StructValue.Fields["current"].NumberValue.Should().Be(16);
    }

    [Fact]
    public async Task ApplyEffect_UnknownEffectType_ReturnsFailureNotException()
    {
        var request = new ApplyEffectRequest
        {
            RequestId = "req-3",
            CampaignId = "campaign-1",
            Actor = MakeActor(),
            Effect = new Struct { Fields = { ["effectType"] = ProtoValue.ForString("mind-control") } },
        };

        var response = await _service.ApplyEffect(request, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResolveCheck_AbilityCheck_ReturnsSuccessfulOutcome()
    {
        var request = new ResolveCheckRequest
        {
            RequestId = "req-4",
            CampaignId = "campaign-1",
            Actor = MakeActor(),
            Params = new Struct { Fields = { ["checkType"] = ProtoValue.ForString("ability_check"), ["ability"] = ProtoValue.ForString("Strength") } },
        };

        var response = await _service.ResolveCheck(request, null!);

        response.Success.Should().BeTrue();
        response.Outcome.Total.Should().BeGreaterThan(0);
        // ICheckManager now returns the full roll detail (not just the
        // total — see ICheckManager.RollAbilityCheck's return docs), so
        // the sidecar can populate the actual d20 face(s) rolled.
        response.Outcome.Rolls.Should().HaveCount(1);
        response.Outcome.Rolls[0].Sides.Should().Be(20);
        response.Outcome.Rolls[0].Result.Should().BeInRange(1, 20);
        response.Outcome.Rolls[0].Label.Should().Be("d20");
    }

    [Fact]
    public async Task ResolveCheck_MissingAbility_ReturnsFailureNotException()
    {
        var request = new ResolveCheckRequest
        {
            RequestId = "req-5",
            CampaignId = "campaign-1",
            Actor = MakeActor(),
            Params = new Struct { Fields = { ["checkType"] = ProtoValue.ForString("ability_check") } },
        };

        var response = await _service.ResolveCheck(request, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidateCharacter_WellFormedCharacterData_ReturnsNoWarnings()
    {
        var actor = MakeActor();
        var request = new ValidateCharacterRequest
        {
            CharacterData = actor.CharacterData,
            SchemaVersion = ActorMapping.SchemaVersion,
        };

        var response = await _service.ValidateCharacter(request, null!);

        response.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateCharacter_MalformedCharacterData_ReturnsErrorWarning()
    {
        var request = new ValidateCharacterRequest
        {
            CharacterData = new Struct(),
            SchemaVersion = ActorMapping.SchemaVersion,
        };

        var response = await _service.ValidateCharacter(request, null!);

        response.Warnings.Should().ContainSingle(w => w.Severity == "error");
    }

    [Fact]
    public async Task StreamEvents_ReturnsUnimplementedRpcException()
    {
        var act = () => _service.StreamEvents(new StreamEventsRequest { CampaignId = "c1" }, null!, null!);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Unimplemented);
    }
}
