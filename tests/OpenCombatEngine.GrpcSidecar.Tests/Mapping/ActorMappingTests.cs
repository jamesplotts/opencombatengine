// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.ObjectModel;
using FluentAssertions;
using Layforge.Protocol.SystemEngine.V1;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.GrpcSidecar.Mapping;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Spells;

namespace OpenCombatEngine.GrpcSidecar.Tests.Mapping;

/// <summary>
/// Tests for <see cref="ActorMapping"/>, which converts between the
/// System Engine gRPC contract's <see cref="Actor"/> message and a live
/// <see cref="StandardCreature"/>.
/// </summary>
public class ActorMappingTests
{
    private static readonly ISpellRepository EmptySpellRepository = new InMemorySpellRepository();

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

        var result = ActorMapping.ToCreature(actor, EmptySpellRepository);

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

        var result = ActorMapping.ToCreature(actor, EmptySpellRepository);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    // The two tests below are the regression coverage for the bug this
    // parameter was added to fix: spellcasting state silently came back
    // null after every gRPC round trip, because ToCreature never had a
    // real ISpellRepository to resolve spell names against. See
    // SystemEngineGrpcService's constructor doc comment and Program.cs's
    // startup wiring for where a real, Open5e-populated repository comes
    // from outside tests.

    [Fact]
    public void ToCreature_SpellcastingWithKnownSpell_ResolvesAgainstRepository()
    {
        var magicMissile = Substitute.For<ISpell>();
        magicMissile.Name.Returns("Magic Missile");
        var repository = new InMemorySpellRepository();
        repository.AddSpell(magicMissile);

        var state = MakeState() with
        {
            Spellcasting = new SpellCasterState(
                CastingAbility: Ability.Intelligence,
                IsPreparedCaster: true,
                KnownSpellNames: new Collection<string> { "Magic Missile" },
                PreparedSpellNames: new Collection<string> { "Magic Missile" },
                Slots: new Collection<SpellSlotState> { new(Level: 1, Max: 3, Current: 3) },
                PactSlotsMax: 0,
                PactSlotsCurrent: 0,
                PactSlotLevel: 0),
        };
        var json = CreatureStateJson.Serialize(state);
        var actor = new Actor
        {
            ActorId = state.Id.ToString(),
            SchemaVersion = ActorMapping.SchemaVersion,
            CharacterData = StructJson.FromJson(json),
        };

        var result = ActorMapping.ToCreature(actor, repository);

        result.IsSuccess.Should().BeTrue();
        result.Value.Spellcasting.Should().NotBeNull();
        result.Value.Spellcasting!.KnownSpells.Should().Contain(s => s.Name == "Magic Missile");
        result.Value.Spellcasting.PreparedSpells.Should().Contain(s => s.Name == "Magic Missile");
    }

    [Fact]
    public void ToCreature_SpellcastingWithUnresolvableSpellName_DropsItRatherThanFailing()
    {
        // Deliberately an empty repository — "fireball" was never loaded
        // (a typo, a non-SRD/homebrew name, or simply not yet fetched).
        // StandardSpellCaster's own state-restoring constructor documents
        // this as "dropped rather than failing the whole restore"; this
        // test exercises that behavior through the actual mapping layer
        // ToCreature sits in front of, not just at the unit level.
        var state = MakeState() with
        {
            Spellcasting = new SpellCasterState(
                CastingAbility: Ability.Intelligence,
                IsPreparedCaster: true,
                KnownSpellNames: new Collection<string> { "Fireball" },
                PreparedSpellNames: new Collection<string>(),
                Slots: new Collection<SpellSlotState>(),
                PactSlotsMax: 0,
                PactSlotsCurrent: 0,
                PactSlotLevel: 0),
        };
        var json = CreatureStateJson.Serialize(state);
        var actor = new Actor
        {
            ActorId = state.Id.ToString(),
            SchemaVersion = ActorMapping.SchemaVersion,
            CharacterData = StructJson.FromJson(json),
        };

        var result = ActorMapping.ToCreature(actor, EmptySpellRepository);

        result.IsSuccess.Should().BeTrue();
        result.Value.Spellcasting.Should().NotBeNull();
        result.Value.Spellcasting!.KnownSpells.Should().BeEmpty();
    }
}
