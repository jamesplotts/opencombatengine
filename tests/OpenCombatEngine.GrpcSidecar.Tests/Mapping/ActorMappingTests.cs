// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.ObjectModel;
using FluentAssertions;
using Layforge.Protocol.SystemEngine.V1;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.GrpcSidecar.Mapping;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
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

    // Regression coverage for a bug found during character-creation live
    // verification (design doc §9.4): a rolled character's player-chosen
    // Gender came back null after StartCharacterCreation/
    // AnswerCharacterCreationPrompt, even though the CreatureState the
    // creation service itself built had the right value. The loss was in
    // this exact ToActor path — StandardCreature's CreatureState-restoring
    // constructor never captured Gender, so its own GetState() could not
    // put it back — not in CreatureStateJson (already covered by
    // CreatureStateJsonTests, which serializes CreatureState directly and
    // never exercises the StandardCreature round trip in between).
    [Fact]
    public void ToActor_StandardCreatureFromStateWithGender_PreservesGenderThroughRoundTrip()
    {
        var creature = new StandardCreature(MakeState() with { Gender = "Female" });

        var actor = ActorMapping.ToActor(creature);

        actor.CharacterData.Fields["gender"].StringValue.Should().Be("Female");
    }

    [Fact]
    public void ToActor_StandardCreatureFromStateWithRaceName_PreservesRaceNameThroughRoundTrip()
    {
        // Same StandardCreature-restoring-constructor path Gender needed a
        // fix for — RaceName rides through it the same way.
        var creature = new StandardCreature(MakeState() with { RaceName = "Dwarf" });

        var actor = ActorMapping.ToActor(creature);

        actor.CharacterData.Fields["raceName"].StringValue.Should().Be("Dwarf");
    }

    [Fact]
    public void ToActor_StandardCreatureFromStateWithChecks_PreservesProficienciesThroughRoundTrip()
    {
        // Regression coverage for a real bug: skill/saving-throw
        // proficiencies were never captured by StandardCreature's
        // CreatureState-restoring constructor at all before
        // CheckManagerState existed — see its own doc comment. Same
        // "restore, then re-export" path Gender/RaceName/Background
        // needed a fix for.
        var creature = new StandardCreature(MakeState() with
        {
            Checks = new CheckManagerState(
                new Collection<string> { "Persuasion" },
                new Collection<Ability> { Ability.Wisdom }),
        });

        var actor = ActorMapping.ToActor(creature);
        var result = ActorMapping.ToCreature(actor, EmptySpellRepository);

        result.IsSuccess.Should().BeTrue();
        result.Value.Checks.HasSkillProficiency("Persuasion").Should().BeTrue();
        result.Value.Checks.HasSavingThrowProficiency(Ability.Wisdom).Should().BeTrue();
        result.Value.Checks.HasSkillProficiency("Athletics").Should().BeFalse();
    }

    [Fact]
    public void ToActor_StandardCreatureFromStateWithBackground_PreservesBackgroundThroughRoundTrip()
    {
        // Same StandardCreature-restoring-constructor path Gender/RaceName
        // needed a fix for — Background rides through it the same way.
        var creature = new StandardCreature(MakeState() with { Background = "Criminal" });

        var actor = ActorMapping.ToActor(creature);

        actor.CharacterData.Fields["background"].StringValue.Should().Be("Criminal");
    }

    // Actor.level (layforge design doc §9.4's character-import review
    // flow) is a plain top-level field precisely so Master can read a
    // character's total level without parsing character_data's
    // engine-specific shape — see ToActor's own reasoning. It must sum
    // every class level for a multiclass character, not just report the
    // first.
    [Fact]
    public void ToActor_MulticlassCreature_LevelIsSumOfEveryClass()
    {
        var state = MakeState() with
        {
            LevelManager = new LevelManagerState(
                ExperiencePoints: 0,
                Classes: new Collection<ClassLevelState> { new("Fighter", 3, 10), new("Wizard", 2, 6) }),
        };
        var creature = new StandardCreature(state);

        var actor = ActorMapping.ToActor(creature);

        actor.Level.Should().Be(5);
    }

    // A creature with no class levels at all (e.g. a monster stat block
    // with only a challenge_rating) reports Level 0 — "unknown/not
    // applicable," per the proto field's own doc comment, never a real
    // level.
    [Fact]
    public void ToActor_CreatureWithNoClassLevels_LevelIsZero()
    {
        var creature = new StandardCreature(MakeState());

        var actor = ActorMapping.ToActor(creature);

        actor.Level.Should().Be(0);
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

    // The two tests below are the regression coverage for the second half
    // of that same bug class, found while wiring the Attack RPC: an
    // equipped weapon also silently came back unequipped after every gRPC
    // round trip, because ToCreature never had a real IItemLibrary to
    // resolve item names against either — StandardCreature.ResolveItem
    // falls back to a bare non-weapon StandardItem placeholder without
    // one, so StandardEquipmentManager.EquipMainHandInternal's own
    // `item is IWeapon` check silently fails and Equipment.MainHand comes
    // back null regardless of what was actually equipped before
    // serialization. See ActorMapping.ToCreature's itemLibrary parameter
    // doc comment and Program.cs's startup wiring for where a real,
    // Open5e-populated library comes from outside tests.

    [Fact]
    public void ToCreature_WithItemLibrary_MainHandWeaponSurvivesRoundTrip()
    {
        var library = Substitute.For<IItemLibrary>();
        var longsword = new StandardWeapon(Guid.NewGuid(), "Longsword", "A longsword.", 3, 15,
            ItemRarity.Common, "1d8", DamageType.Slashing, new[] { WeaponProperty.Versatile }, range: 5);
        library.GetItem("Longsword").Returns(longsword);

        var state = MakeState() with
        {
            Inventory = new InventoryState(new Collection<ItemInstanceState> { new("Longsword") }),
            Equipment = new EquipmentState(
                new Collection<EquippedSlotState> { new(OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand, 0) },
                new Collection<int>()),
        };
        var json = CreatureStateJson.Serialize(state);
        var actor = new Actor
        {
            ActorId = state.Id.ToString(),
            SchemaVersion = ActorMapping.SchemaVersion,
            CharacterData = StructJson.FromJson(json),
        };

        var result = ActorMapping.ToCreature(actor, EmptySpellRepository, library);

        result.IsSuccess.Should().BeTrue();
        result.Value.Equipment.MainHand.Should().NotBeNull("a real IItemLibrary was supplied, so the equipped item should resolve as a real IWeapon");
        result.Value.Equipment.MainHand!.Name.Should().Be("Longsword");
        result.Value.Equipment.MainHand.Range.Should().Be(5);
    }

    [Fact]
    public void ToCreature_WithoutItemLibrary_MainHandWeaponComesBackNull()
    {
        // Explicit regression proof of the bug itself, not just its fix:
        // omitting itemLibrary (the default, and every call site's
        // behavior before this parameter existed) must still reproduce
        // the original silent-drop behavior exactly — same reasoning as
        // CastSpell_NoGridContext_SkipsRangeCheck's own "prove the
        // omitted-parameter path is unchanged" tests.
        var state = MakeState() with
        {
            Inventory = new InventoryState(new Collection<ItemInstanceState> { new("Longsword") }),
            Equipment = new EquipmentState(
                new Collection<EquippedSlotState> { new(OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand, 0) },
                new Collection<int>()),
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
        result.Value.Equipment.MainHand.Should().BeNull();
    }

    // Extends the "one level deep" coverage CombatSerializationTests
    // already has for CombatSerializer's own internal save/load path —
    // this is the SAME nested-container mechanism
    // (ItemInstanceState.Contents/IContainer), but exercised through the
    // actual external wire path (Actor.character_data) the item-carry-
    // location feature uses in production, which had zero coverage
    // before this. Four deep — Potion in Flask in Purse in Pack —
    // directly answers the "does stowed-in-a-container survive
    // save/load at arbitrary nesting depth" concern this feature was
    // built to settle.
    [Fact]
    public void ToActor_Then_ToCreature_RoundTripsDeeplyNestedContainerContents()
    {
        var libraryPack = new ContainerItem("Pack", baseWeight: 5, weightCapacity: 50);
        var libraryPurse = new ContainerItem("Purse", baseWeight: 0.5, weightCapacity: 5);
        var libraryFlask = new ContainerItem("Flask", baseWeight: 0.5, weightCapacity: 2);
        var libraryPotion = new StandardItem(Guid.NewGuid(), "Potion", "A potion.", 0.5, 50);
        // Restore resolves each nested item's name independently against
        // the library (StandardCreature.ResolveItem recurses into
        // itemState.Contents calling itself again) — every name in the
        // chain needs its own entry, not just the top-level one.
        var library = Substitute.For<IItemLibrary>();
        library.GetItem("Pack").Returns(libraryPack);
        library.GetItem("Purse").Returns(libraryPurse);
        library.GetItem("Flask").Returns(libraryFlask);
        library.GetItem("Potion").Returns(libraryPotion);

        // Built from live objects directly (CombatSerializationTests'
        // own style), not restored from state — so the one and only
        // restore this test exercises happens inside ToCreature below.
        var originalCreature = new StandardCreature(MakeState());
        var pack = new ContainerItem("Pack", baseWeight: 5, weightCapacity: 50);
        var purse = new ContainerItem("Purse", baseWeight: 0.5, weightCapacity: 5);
        var flask = new ContainerItem("Flask", baseWeight: 0.5, weightCapacity: 2);
        var potion = new StandardItem(Guid.NewGuid(), "Potion", "A potion.", 0.5, 50);
        flask.AddItem(potion);
        purse.AddItem(flask);
        pack.AddItem(purse);
        originalCreature.Inventory.AddItem(pack);

        var actor = ActorMapping.ToActor(originalCreature);
        var result = ActorMapping.ToCreature(actor, EmptySpellRepository, library);

        result.IsSuccess.Should().BeTrue();
        var restoredPack = result.Value.Inventory.Items.Single() as IContainer;
        restoredPack.Should().NotBeNull();
        // Confirms the tree was genuinely REBUILT from the library's own
        // instances rather than just coincidentally sharing the original
        // live object graph — the same regression shape
        // Should_Save_And_Load_Nested_Container_Contents already guards
        // for the internal CombatSerializer path.
        ReferenceEquals(restoredPack, pack).Should().BeFalse();
        var restoredPurse = restoredPack!.Contents.Single() as IContainer;
        restoredPurse.Should().NotBeNull();
        var restoredFlask = restoredPurse!.Contents.Single() as IContainer;
        restoredFlask.Should().NotBeNull();
        restoredFlask!.Contents.Should().ContainSingle(i => i.Name == "Potion");
    }
}
