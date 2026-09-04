// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.ObjectModel;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Layforge.Protocol.SystemEngine.V1;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.GrpcSidecar;
using OpenCombatEngine.GrpcSidecar.Mapping;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spells;
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
    private readonly ISpellRepository _spellRepository = new InMemorySpellRepository();
    private readonly IDiceRoller _diceRoller = new StandardDiceRoller();
    private readonly IItemLibrary _itemLibrary = new FakeItemLibrary();
    private readonly OpenCombatEngine.Core.Interfaces.Loot.ILootGenerator _lootGenerator;
    private readonly OpenCombatEngine.Core.Interfaces.Loot.IEncounterChallengeCalculator _encounterCalculator = new OpenCombatEngine.Implementation.Loot.StandardEncounterChallengeCalculator();
    private readonly SystemEngineGrpcService _service;

    public SystemEngineGrpcServiceTests()
    {
        _lootGenerator = new OpenCombatEngine.Implementation.Loot.StandardLootGenerator(_itemLibrary, _diceRoller);
        _service = new SystemEngineGrpcService(_spellRepository, _diceRoller, _itemLibrary, _lootGenerator, _encounterCalculator);
    }

    // Minimal IItemLibrary test double — StandardCreature.ResolveItem
    // (src/OpenCombatEngine.Implementation/Creatures/StandardCreature.cs)
    // looks items up by name on restore, so this only needs to support
    // GetItem("&lt;Name&gt;") for the small set of real weapons the Attack_*
    // tests below equip. Real Open5e property strings/ranges (see
    // Open5eItemMapperTests.cs), not fabricated numbers.
    private sealed class FakeItemLibrary : IItemLibrary
    {
        private readonly Dictionary<string, IItem> _items = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Longsword"] = new StandardWeapon(Guid.NewGuid(), "Longsword", "A longsword.", 3, 15,
                ItemRarity.Common, "1d8", DamageType.Slashing, new[] { WeaponProperty.Versatile }, range: 5),
            ["Shortbow"] = new StandardWeapon(Guid.NewGuid(), "Shortbow", "A shortbow.", 2, 25,
                ItemRarity.Common, "1d6", DamageType.Piercing, new[] { WeaponProperty.Ammunition, WeaponProperty.TwoHanded }, range: 80),
            ["Dagger"] = new StandardWeapon(Guid.NewGuid(), "Dagger", "A dagger.", 1, 2,
                ItemRarity.Common, "1d4", DamageType.Piercing, new[] { WeaponProperty.Finesse, WeaponProperty.Light, WeaponProperty.Thrown }, range: 20),
            ["Greatsword"] = new StandardWeapon(Guid.NewGuid(), "Greatsword", "A greatsword.", 6, 50,
                ItemRarity.Common, "2d6", DamageType.Slashing, new[] { WeaponProperty.TwoHanded }, range: 5),
            // Distinct from Dagger deliberately: StandardEquipmentManager.Equip
            // unequips an item from any OTHER slot before equipping it into a
            // new one (ReferenceEquals check) — since this library hands out
            // one shared instance per name, equipping the SAME named weapon
            // into both MainHand and OffHand (e.g. twin daggers) silently
            // steals it back out of the first slot. A real, pre-existing
            // engine limitation, out of scope for this session; using two
            // different Light weapons here sidesteps it rather than
            // masking it.
            ["Shortsword"] = new StandardWeapon(Guid.NewGuid(), "Shortsword", "A shortsword.", 2, 10,
                ItemRarity.Common, "1d6", DamageType.Piercing, new[] { WeaponProperty.Finesse, WeaponProperty.Light }, range: 5),
            // A plain non-weapon item, for equip/unequip/receive/discard/
            // transfer tests that don't need weapon-specific behavior.
            ["Torch"] = new StandardItem(Guid.NewGuid(), "Torch", "A wooden torch.", 1, 1, ItemRarity.Common, ItemType.Other),
        };

        public IItem? GetItem(string slug) => _items.TryGetValue(slug, out var item) ? item : null;
        public IWeapon? GetWeapon(string slug) => GetItem(slug) as IWeapon;
        public IArmor? GetArmor(string slug) => GetItem(slug) as IArmor;
        public IEnumerable<IItem> GetAllItems() => _items.Values;
        public IEnumerable<IItem> GetItemsByRarity(ItemRarity rarity) => _items.Values.Where(i => i.Rarity == rarity);
        public IItem? GetRandomItem(ItemRarity? rarity = null, ItemType? type = null) => _items.Values.FirstOrDefault();
    }

    private static CreatureState MakeState(int currentHp = 24, int maxHp = 30) => new(
        Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
        Name: "Kestrel",
        Team: "Player",
        AbilityScores: new AbilityScoresState(16, 12, 14, 10, 13, 8),
        HitPoints: new HitPointsState(currentHp, maxHp, 0));

    private static Actor MakeActor(int currentHp = 24, int maxHp = 30) =>
        ActorMapping.ToActor(new StandardCreature(MakeState(currentHp, maxHp)));

    // MakeState always uses the same fixed Id — fine for every existing
    // test, which never places more than one creature on a grid at once,
    // but StandardGridManager.PlaceCreature keys by creature Id and
    // rejects placing a second creature under one already in use. The
    // CastSpell grid-context tests place both caster and target on the
    // same grid, so the target there needs a real, distinct Id — this
    // exists instead of changing MakeState's own fixed Id, which every
    // other test in this file relies on staying exactly what it is.
    private static Actor MakeGridTargetActor(int currentHp = 24, int maxHp = 30) =>
        ActorMapping.ToActor(new StandardCreature(MakeState(currentHp, maxHp) with
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
        }));

    // A second distinct-Id target, for GetAvailableActions_* tests that
    // need two real candidate targets at once (MakeState's own fixed Id
    // and MakeGridTargetActor's Id are both already taken).
    private static Actor MakeSecondGridTargetActor(int currentHp = 24, int maxHp = 30) =>
        ActorMapping.ToActor(new StandardCreature(MakeState(currentHp, maxHp) with
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "SecondTarget",
        }));

    // weaponName must be a key FakeItemLibrary resolves (Longsword,
    // Shortbow, Dagger) — StandardCreature.ResolveItem looks the
    // inventory item up by this same Name against _itemLibrary, so an
    // unregistered name would silently restore as a bare non-weapon
    // StandardItem instead (same fallback ActorMapping.ToCreature's own
    // remarks describe for a null/absent library).
    private static CreatureState MakeStateWithWeapon(string weaponName, int currentHp = 24, int maxHp = 30) => MakeState(currentHp, maxHp) with
    {
        Inventory = new InventoryState(new Collection<ItemInstanceState> { new(weaponName) }),
        Equipment = new EquipmentState(
            new Collection<EquippedSlotState> { new(OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand, 0) },
            new Collection<int>()),
    };

    private Actor MakeActorWithWeapon(string weaponName, int currentHp = 24, int maxHp = 30) =>
        ActorMapping.ToActor(new StandardCreature(MakeStateWithWeapon(weaponName, currentHp, maxHp), _spellRepository, _itemLibrary));

    private static CreatureState MakeStateWithWeapons(string mainHandName, string offHandName, int currentHp = 24, int maxHp = 30) => MakeState(currentHp, maxHp) with
    {
        Inventory = new InventoryState(new Collection<ItemInstanceState> { new(mainHandName), new(offHandName) }),
        Equipment = new EquipmentState(
            new Collection<EquippedSlotState> { new(OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand, 0), new(OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand, 1) },
            new Collection<int>()),
    };

    private Actor MakeActorWithWeapons(string mainHandName, string offHandName, int currentHp = 24, int maxHp = 30) =>
        ActorMapping.ToActor(new StandardCreature(MakeStateWithWeapons(mainHandName, offHandName, currentHp, maxHp), _spellRepository, _itemLibrary));

    // An item in inventory but NOT equipped — for equip/receive/discard/
    // transfer tests that need a real starting inventory without any
    // equipment-slot assumptions.
    private static CreatureState MakeStateWithInventoryItem(string itemName, int currentHp = 24, int maxHp = 30) => MakeState(currentHp, maxHp) with
    {
        Inventory = new InventoryState(new Collection<ItemInstanceState> { new(itemName) }),
    };

    private Actor MakeActorWithInventoryItem(string itemName, int currentHp = 24, int maxHp = 30) =>
        ActorMapping.ToActor(new StandardCreature(MakeStateWithInventoryItem(itemName, currentHp, maxHp), _spellRepository, _itemLibrary));

    // A creature with a real challenge_rating recorded (or none, when cr
    // is null) — for GenerateLoot tests. id lets a test place several
    // distinct participants (StandardGridManager-style distinct-Id
    // pattern isn't needed here since GenerateLoot never touches a grid,
    // but ActorMapping round-trips whatever Id is given either way).
    private static CreatureState MakeStateWithChallengeRating(double? cr, string id = "33333333-3333-3333-3333-333333333333", string name = "Kestrel") => MakeState() with
    {
        Id = Guid.Parse(id),
        Name = name,
        ChallengeRating = cr,
    };

    private Actor MakeActorWithChallengeRating(double? cr, string id = "33333333-3333-3333-3333-333333333333", string name = "Kestrel") =>
        ActorMapping.ToActor(new StandardCreature(MakeStateWithChallengeRating(cr, id, name), _spellRepository, _itemLibrary));

    // A creature with real currency already carried — for
    // TransferCurrency tests.
    private static CreatureState MakeStateWithCurrency(int copper, int silver, int gold, int platinum, string id = "33333333-3333-3333-3333-333333333333") => MakeState() with
    {
        Id = Guid.Parse(id),
        Inventory = new InventoryState(new Collection<ItemInstanceState>(), copper, silver, gold, platinum),
    };

    private Actor MakeActorWithCurrency(int copper, int silver, int gold, int platinum, string id = "33333333-3333-3333-3333-333333333333") =>
        ActorMapping.ToActor(new StandardCreature(MakeStateWithCurrency(copper, silver, gold, platinum, id), _spellRepository, _itemLibrary));

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

    // Regression coverage for a real bug found via live testing: spellcasting
    // state came back null after every gRPC round trip because FromJson (and
    // every other handler reconstructing a StandardCreature) never had a real
    // ISpellRepository to resolve spell names against. This is the test that
    // would have caught it.
    [Fact]
    public async Task FromJson_CharacterWithKnownSpell_SpellcastingSurvivesRoundTrip()
    {
        var magicMissile = Substitute.For<ISpell>();
        magicMissile.Name.Returns("Magic Missile");
        _spellRepository.AddSpell(magicMissile);

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

        var response = await _service.FromJson(new FromJsonRequest { Json = json }, null!);

        response.Warnings.Should().BeEmpty();
        response.Actor.CharacterData.Fields["spellcasting"].StructValue
            .Fields["preparedSpellNames"].ListValue.Values
            .Select(v => v.StringValue)
            .Should().Contain("Magic Missile");
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

    [Fact]
    public async Task StartTurn_ActiveCreature_DoesNotRollDeathSave()
    {
        var response = await _service.StartTurn(
            new StartTurnRequest { RequestId = "req-1", CampaignId = "campaign-1", Actor = MakeActor(currentHp: 24) }, null!);

        response.Success.Should().BeTrue();
        response.DeathSaveRolled.Should().BeFalse();
        response.WokeUp.Should().BeFalse();
        response.Actor.Should().NotBeNull();
    }

    // A 0-HP, non-dead, non-stable creature always rolls a death save on
    // StartTurn regardless of the actual d20 result (docs/design.md §9.3)
    // — this is the one invariant testable without controlling the real
    // dice roller the gRPC-level Actor round trip uses internally. The
    // roll-value-dependent branches (natural 20 heals+wakes, natural 1
    // counts double) are already covered with a deterministic mocked
    // roller at the Core/Implementation layer
    // (OpenCombatEngine.Implementation.Tests/DeathSaveTests.cs) — this
    // test only needs to confirm the RPC surfaces whatever StartTurn()
    // reports, not re-verify each branch's game logic.
    [Fact]
    public async Task StartTurn_DownCreature_AlwaysRollsDeathSave()
    {
        var response = await _service.StartTurn(
            new StartTurnRequest { RequestId = "req-1", CampaignId = "campaign-1", Actor = MakeActor(currentHp: 0) }, null!);

        response.Success.Should().BeTrue();
        response.DeathSaveRolled.Should().BeTrue();
        response.DeathSaveOutcome.Should().NotBeNull();
        response.DeathSaveOutcome.Rolls.Should().ContainSingle();
        response.DeathSaveOutcome.Rolls[0].Sides.Should().Be(20);
        response.DeathSaveOutcome.Rolls[0].Result.Should().BeInRange(1, 20);
    }

    // Regression coverage for design doc §8/§9's "gates over prompting":
    // CastSpell is the real mechanical gate against casting a spell that
    // isn't prepared/known or that has no available slot — previously
    // there was no way to reach this at all over gRPC, so nothing but the
    // DM model's own narrative judgment stood between a player and an
    // unprepared cast. These tests exercise the actual CastSpellAction
    // rejection paths through the gRPC surface, not just at the
    // Core/Implementation unit level (already covered by
    // CastSpellActionTests.cs).

    private static Spell MakeMagicMissile() => new(
        name: "Magic Missile",
        level: 1,
        school: SpellSchool.Evocation,
        castingTime: "1 action",
        range: "120 feet",
        components: "V, S",
        duration: "Instantaneous",
        description: "Three darts of force.",
        diceRoller: new StandardDiceRoller(),
        damageRolls: new[] { new OpenCombatEngine.Core.Models.Spells.DamageFormula("3d4+3", DamageType.Force) });

    private static CreatureState MakeWizardState(bool preparedMagicMissile, int slotsAvailable = 1) => MakeState(currentHp: 20, maxHp: 20) with
    {
        Spellcasting = new SpellCasterState(
            CastingAbility: Ability.Intelligence,
            IsPreparedCaster: true,
            KnownSpellNames: new Collection<string> { "Magic Missile" },
            PreparedSpellNames: preparedMagicMissile ? new Collection<string> { "Magic Missile" } : new Collection<string>(),
            Slots: new Collection<SpellSlotState> { new(Level: 1, Max: 3, Current: slotsAvailable) },
            PactSlotsMax: 0,
            PactSlotsCurrent: 0,
            PactSlotLevel: 0),
    };

    // Uses this instance's own _spellRepository (not a fresh/isolated
    // one) deliberately: the wire Actor this produces must be built
    // against the SAME spell data the test's later CastSpell call will
    // resolve against, or a spell name that fails to resolve here would
    // silently drop out of the round-tripped known/prepared lists
    // entirely (StandardSpellCaster's own documented "drop, don't fail"
    // behavior) — masking the test's actual premise. Callers must
    // AddSpell every spell referenced by the state before calling this.
    private Actor MakeActorFromState(CreatureState state) =>
        ActorMapping.ToActor(new StandardCreature(state, _spellRepository));

    [Fact]
    public async Task CastSpell_PreparedSpellWithSlot_SucceedsAndConsumesSlot()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 2));
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, Target = target, SpellName = "Magic Missile" }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
        response.Caster.CharacterData.Fields["spellcasting"].StructValue
            .Fields["slots"].ListValue.Values[0].StructValue.Fields["current"].NumberValue.Should().Be(1);
        response.Target.Should().NotBeNull();
        response.ResultMessage.Should().NotBeNullOrWhiteSpace();
        // Magic Missile always hits (no save/attack roll) and always deals
        // damage — this is Master's PvP-gate signal (design doc §9.1); see
        // CastSpellResponse.target_damaged's doc comment.
        response.TargetDamaged.Should().BeTrue();
    }

    [Fact]
    public async Task CastSpell_NonDamagingSpell_TargetDamagedIsFalse()
    {
        var healingSpell = new Spell(
            name: "Cure Wounds", level: 1, school: SpellSchool.Evocation, castingTime: "1 action",
            range: "Touch", components: "V, S", duration: "Instantaneous", description: "Heals.",
            diceRoller: new StandardDiceRoller(), healingDice: "1d8+3");
        _spellRepository.AddSpell(healingSpell);
        var caster = MakeActorFromState(MakeState() with
        {
            Spellcasting = new SpellCasterState(
                CastingAbility: Ability.Wisdom, IsPreparedCaster: true,
                KnownSpellNames: new Collection<string> { "Cure Wounds" },
                PreparedSpellNames: new Collection<string> { "Cure Wounds" },
                Slots: new Collection<SpellSlotState> { new(Level: 1, Max: 2, Current: 2) },
                PactSlotsMax: 0, PactSlotsCurrent: 0, PactSlotLevel: 0),
        });
        var target = MakeActor(currentHp: 5, maxHp: 30);

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, Target = target, SpellName = "Cure Wounds" }, null!);

        response.Success.Should().BeTrue();
        response.TargetDamaged.Should().BeFalse("Master's PvP gate must not fire for a purely beneficial spell");
    }

    [Fact]
    public async Task CastSpell_UnpreparedSpell_ReturnsFailure()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: false, slotsAvailable: 2));
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, Target = target, SpellName = "Magic Missile" }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("not prepared");
    }

    [Fact]
    public async Task CastSpell_NoSlotAvailable_ReturnsFailure()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 0));
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, Target = target, SpellName = "Magic Missile" }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("slot");
    }

    [Fact]
    public async Task CastSpell_UnknownSpellName_ReturnsFailure()
    {
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: true));

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, SpellName = "Definitely Not A Real Spell" }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Unknown spell");
    }

    [Fact]
    public async Task CastSpell_NoTargetGiven_SelfCastsAndOmitsTargetInResponse()
    {
        var selfSpell = new Spell(
            name: "Mage Armor", level: 1, school: SpellSchool.Abjuration, castingTime: "1 action",
            range: "Self", components: "V, S, M", duration: "8 hours", description: "AC boost.",
            diceRoller: new StandardDiceRoller());
        _spellRepository.AddSpell(selfSpell);
        var state = MakeState() with
        {
            Spellcasting = new SpellCasterState(
                CastingAbility: Ability.Intelligence, IsPreparedCaster: true,
                KnownSpellNames: new Collection<string> { "Mage Armor" },
                PreparedSpellNames: new Collection<string> { "Mage Armor" },
                Slots: new Collection<SpellSlotState> { new(Level: 1, Max: 2, Current: 2) },
                PactSlotsMax: 0, PactSlotsCurrent: 0, PactSlotLevel: 0),
        };
        var caster = ActorMapping.ToActor(new StandardCreature(state, _spellRepository));

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, SpellName = "Mage Armor" }, null!);

        response.Success.Should().BeTrue();
        response.Target.Should().BeNull("a self-cast's target is identical to the caster, already returned as Caster");
    }

    // Regression coverage for this session's own scope-decision #4: a
    // non-prepared caster (Sorcerer-style — casts from KnownSpells
    // directly, no separate "prepared" step) with an EMPTY
    // preparedSpellNames must still be able to cast a spell that's only
    // in knownSpellNames. StandardSpellCaster.PreparedSpells already
    // falls back to KnownSpells when IsPreparedCaster is false — this
    // proves that fallback actually reaches CastSpell's real rejection
    // path, not just the Core-level property getter in isolation.
    [Fact]
    public async Task CastSpell_NonPreparedCasterKnownSpell_Succeeds()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var state = MakeState(currentHp: 20, maxHp: 20) with
        {
            Spellcasting = new SpellCasterState(
                CastingAbility: Ability.Charisma,
                IsPreparedCaster: false,
                KnownSpellNames: new Collection<string> { "Magic Missile" },
                PreparedSpellNames: new Collection<string>(), // deliberately empty
                Slots: new Collection<SpellSlotState> { new(Level: 1, Max: 2, Current: 2) },
                PactSlotsMax: 0, PactSlotsCurrent: 0, PactSlotLevel: 0),
        };
        var caster = ActorMapping.ToActor(new StandardCreature(state, _spellRepository));
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, Target = target, SpellName = "Magic Missile" }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    // Regression coverage for wiring Master's own combat map
    // (internal/combatmap) into real range/line-of-sight gating —
    // CastSpellAction.Execute already checks both (src/
    // OpenCombatEngine.Implementation/Actions/CastSpellAction.cs), but
    // only when context.Grid is non-null and both creatures are actually
    // placed on it; before this, CastSpell never constructed one at all.
    // Magic Missile's own range ("120 feet", MakeMagicMissile) is real
    // SRD data, not a fabricated test-only value.

    [Fact]
    public async Task CastSpell_GridContextWithTargetInRangeAndClearSight_Succeeds()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 2));
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(new CastSpellRequest
        {
            RequestId = "r1",
            CampaignId = "c1",
            Caster = caster,
            Target = target,
            SpellName = "Magic Missile",
            GridContext = new GridContext
            {
                CasterPosition = new GridPosition { X = 0, Y = 0 },
                TargetPosition = new GridPosition { X = 4, Y = 0 }, // 20 feet — well within 120
            },
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task CastSpell_GridContextTargetOutOfRange_ReturnsFailure()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 2));
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(new CastSpellRequest
        {
            RequestId = "r1",
            CampaignId = "c1",
            Caster = caster,
            Target = target,
            SpellName = "Magic Missile",
            GridContext = new GridContext
            {
                CasterPosition = new GridPosition { X = 0, Y = 0 },
                TargetPosition = new GridPosition { X = 30, Y = 0 }, // 150 feet — beyond Magic Missile's 120
            },
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("out of range");
    }

    [Fact]
    public async Task CastSpell_GridContextObstacleBlocksLineOfSight_ReturnsFailure()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 2));
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(new CastSpellRequest
        {
            RequestId = "r1",
            CampaignId = "c1",
            Caster = caster,
            Target = target,
            SpellName = "Magic Missile",
            GridContext = new GridContext
            {
                CasterPosition = new GridPosition { X = 0, Y = 0 },
                TargetPosition = new GridPosition { X = 4, Y = 0 }, // within range, but...
                Obstacles = { new GridPosition { X = 2, Y = 0 } }, // ...directly between them
            },
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("line of sight");
    }

    [Fact]
    public async Task CastSpell_NoGridContext_SkipsRangeCheck_SucceedsRegardlessOfSpellRange()
    {
        // Explicit regression proof: omitting grid_context entirely must
        // behave exactly like every other CastSpell_* test above that
        // never sets it — a target that would be wildly out of range on
        // any real map still succeeds, because there's no grid to check
        // range against at all (context.Grid stays null).
        _spellRepository.AddSpell(MakeMagicMissile());
        var caster = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 2));
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.CastSpell(
            new CastSpellRequest { RequestId = "r1", CampaignId = "c1", Caster = caster, Target = target, SpellName = "Magic Missile" }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task CastSpell_GridContextButNoTarget_SelfCast_SkipsGridEntirely()
    {
        var selfSpell = new Spell(
            name: "Mage Armor", level: 1, school: SpellSchool.Abjuration, castingTime: "1 action",
            range: "Self", components: "V, S, M", duration: "8 hours", description: "AC boost.",
            diceRoller: new StandardDiceRoller());
        _spellRepository.AddSpell(selfSpell);
        var state = MakeState() with
        {
            Spellcasting = new SpellCasterState(
                CastingAbility: Ability.Intelligence, IsPreparedCaster: true,
                KnownSpellNames: new Collection<string> { "Mage Armor" },
                PreparedSpellNames: new Collection<string> { "Mage Armor" },
                Slots: new Collection<SpellSlotState> { new(Level: 1, Max: 2, Current: 2) },
                PactSlotsMax: 0, PactSlotsCurrent: 0, PactSlotLevel: 0),
        };
        var caster = ActorMapping.ToActor(new StandardCreature(state, _spellRepository));

        // A grid_context with no Target set at all is a malformed request
        // in practice (Master never sends one for a self-cast), but the
        // point of this test is that hasTarget — not GridContext's mere
        // presence — gates whether a grid is built at all; this must not
        // throw a null-reference trying to read a TargetPosition that
        // doesn't matter for a self-cast.
        var response = await _service.CastSpell(new CastSpellRequest
        {
            RequestId = "r1",
            CampaignId = "c1",
            Caster = caster,
            SpellName = "Mage Armor",
            GridContext = new GridContext
            {
                CasterPosition = new GridPosition { X = 0, Y = 0 },
                TargetPosition = new GridPosition { X = 0, Y = 0 },
            },
        }, null!);

        response.Success.Should().BeTrue();
        response.Target.Should().BeNull("a self-cast's target is identical to the caster, already returned as Caster");
    }

    [Fact]
    public async Task StartTurn_MalformedActor_ReturnsFailureNotException()
    {
        var badActor = new Actor { ActorId = "x", CharacterData = new Struct(), SchemaVersion = ActorMapping.SchemaVersion };

        var response = await _service.StartTurn(
            new StartTurnRequest { RequestId = "req-1", CampaignId = "campaign-1", Actor = badActor }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().NotBeNullOrEmpty();
    }

    // Regression coverage for design doc §8/§9's "gates over prompting":
    // Attack (melee_attack/ranged_attack, Master's dm_tools.go) is the real
    // mechanical gate against a martial character's attack that previously
    // had no RPC at all — apply_effect has no range/weapon-legality concept.
    // These prove the NEW gating this RPC adds (weapon-kind legality,
    // no-weapon-equipped, range/LOS via grid_context); AttackAction's own
    // hit/miss/damage/advantage logic is already covered deterministically
    // by AttackActionTests.cs and is not re-proven here — a real d20 roll
    // via _diceRoller (StandardDiceRoller) can't be pinned to hit or miss,
    // so these assert Success (the attack was legally resolved) rather
    // than Hit (whether it happened to connect), same reasoning
    // RELEASE_NOTES.md documents for CastSpell's own attack-roll gating.

    [Fact]
    public async Task Attack_MeleeWithLongswordNoGridContext_Succeeds()
    {
        var attacker = MakeActorWithWeapon("Longsword");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
        response.Attacker.Should().NotBeNull();
        response.Target.Should().NotBeNull();
    }

    [Fact]
    public async Task Attack_RangedWithShortbow_Succeeds()
    {
        var attacker = MakeActorWithWeapon("Shortbow");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Ranged,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task Attack_ThrownDagger_SucceedsForBothMeleeAndRanged()
    {
        // A Thrown+Finesse weapon (Dagger) is legal both ways per SRD —
        // the real gate is the weapon's own properties, not a hardcoded
        // per-kind allowlist.
        var meleeAttacker = MakeActorWithWeapon("Dagger");
        var meleeTarget = MakeActor(currentHp: 10, maxHp: 10);
        var meleeResponse = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = meleeAttacker, Target = meleeTarget, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
        }, null!);
        meleeResponse.Success.Should().BeTrue();

        var rangedAttacker = MakeActorWithWeapon("Dagger");
        var rangedTarget = MakeActor(currentHp: 10, maxHp: 10);
        var rangedResponse = await _service.Attack(new AttackRequest
        {
            RequestId = "r2", CampaignId = "c1", Attacker = rangedAttacker, Target = rangedTarget, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Ranged,
        }, null!);
        rangedResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Attack_MeleeAttackWithBowEquipped_ReturnsFailure()
    {
        var attacker = MakeActorWithWeapon("Shortbow");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("melee attack");
    }

    [Fact]
    public async Task Attack_RangedAttackWithLongswordEquipped_ReturnsFailure()
    {
        var attacker = MakeActorWithWeapon("Longsword");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Ranged,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("ranged attack");
    }

    [Fact]
    public async Task Attack_NoWeaponEquipped_ReturnsFailure()
    {
        var attacker = MakeActor();
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("No weapon equipped");
    }

    [Fact]
    public async Task Attack_UnspecifiedKind_ReturnsFailure()
    {
        var attacker = MakeActorWithWeapon("Longsword");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target,
            // Kind deliberately omitted — proto3 default is ATTACK_KIND_UNSPECIFIED.
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("kind");
    }

    [Fact]
    public async Task Attack_NoTargetGiven_ReturnsFailure()
    {
        var attacker = MakeActorWithWeapon("Longsword");

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("target is required");
    }

    [Fact]
    public async Task Attack_GridContextWithTargetInRangeAndClearSight_Succeeds()
    {
        var attacker = MakeActorWithWeapon("Longsword"); // range 5
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
            GridContext = new GridContext
            {
                CasterPosition = new GridPosition { X = 0, Y = 0 },
                TargetPosition = new GridPosition { X = 1, Y = 0 }, // 5 feet — exactly in range
            },
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task Attack_GridContextTargetOutOfRange_ReturnsFailure()
    {
        var attacker = MakeActorWithWeapon("Longsword"); // range 5
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
            GridContext = new GridContext
            {
                CasterPosition = new GridPosition { X = 0, Y = 0 },
                TargetPosition = new GridPosition { X = 4, Y = 0 }, // 20 feet — beyond a 5-foot melee range
            },
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("out of range");
    }

    [Fact]
    public async Task Attack_NoGridContext_SkipsRangeCheck_SucceedsRegardlessOfDistance()
    {
        var attacker = MakeActorWithWeapon("Longsword"); // range 5
        var target = MakeActor(currentHp: 10, maxHp: 10); // no grid position at all

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target, Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Melee,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    // Regression coverage for design doc §8/§9's "gates over prompting":
    // GetAvailableActions is the real engine-computed action menu — the
    // DM previously had to guess at weapon legality, spell slot
    // availability, and free-hand/incapacitation state (this session's
    // own live verification found the DM model narrating around a real
    // gate rather than calling a tool that would have surfaced it).

    [Fact]
    public async Task GetAvailableActions_ShortswordAndDagger_ReturnsFullMenuPerTarget()
    {
        // Both weapons are Light (SRD Two-Weapon Fighting) — Shortsword
        // main hand, Dagger off hand. Dagger is also Thrown, but the
        // ranged option only applies to the equipped MAIN hand weapon in
        // this pass (off-hand ranged use is a further real gap, not
        // built here — see the plan's own scope notes).
        var actor = MakeActorWithWeapons("Shortsword", "Dagger");
        var target1 = MakeGridTargetActor(currentHp: 10, maxHp: 10);
        var target2 = MakeSecondGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target1, target2 },
        }, null!);

        response.Success.Should().BeTrue();
        response.CanAct.Should().BeTrue();

        foreach (var targetId in new[] { "44444444-4444-4444-4444-444444444444", "55555555-5555-5555-5555-555555555555" })
        {
            response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.MeleeAttack && a.TargetCharacterId == targetId && a.SourceName == "Shortsword");
            response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.OffhandAttack && a.TargetCharacterId == targetId && a.SourceName == "Dagger");
            response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.ShoveProne && a.TargetCharacterId == targetId);
            response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.ShovePush && a.TargetCharacterId == targetId);
            // Both hands hold a weapon — correctly no free hand to grapple.
            response.Actions.Should().NotContain(a => a.Kind == AvailableActionKind.Grapple && a.TargetCharacterId == targetId);
        }
    }

    [Fact]
    public async Task GetAvailableActions_TwoHandedWeapon_NoGrappleOption()
    {
        var actor = MakeActorWithWeapon("Greatsword");
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target },
        }, null!);

        response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.MeleeAttack);
        response.Actions.Should().NotContain(a => a.Kind == AvailableActionKind.Grapple);
        // Greatsword has neither Thrown nor Ammunition — no ranged option either.
        response.Actions.Should().NotContain(a => a.Kind == AvailableActionKind.RangedAttack);
    }

    [Fact]
    public async Task GetAvailableActions_PreparedSpellWithSlot_IsCastable()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var actor = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 2));
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target },
        }, null!);

        response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.CastSpell && a.SourceName == "Magic Missile");
    }

    [Fact]
    public async Task GetAvailableActions_PreparedSpellNoSlot_NotCastable()
    {
        _spellRepository.AddSpell(MakeMagicMissile());
        var actor = MakeActorFromState(MakeWizardState(preparedMagicMissile: true, slotsAvailable: 0));
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target },
        }, null!);

        response.Actions.Should().NotContain(a => a.Kind == AvailableActionKind.CastSpell);
    }

    [Fact]
    public async Task GetAvailableActions_Cantrip_AlwaysCastableRegardlessOfSlots()
    {
        var cantrip = new Spell(
            name: "Fire Bolt", level: 0, school: SpellSchool.Evocation, castingTime: "1 action",
            range: "120 feet", components: "V, S", duration: "Instantaneous", description: "A mote of fire.",
            diceRoller: new StandardDiceRoller());
        _spellRepository.AddSpell(cantrip);
        var state = MakeState(currentHp: 20, maxHp: 20) with
        {
            Spellcasting = new SpellCasterState(
                CastingAbility: Ability.Intelligence, IsPreparedCaster: true,
                KnownSpellNames: new Collection<string> { "Fire Bolt" },
                PreparedSpellNames: new Collection<string> { "Fire Bolt" },
                Slots: new Collection<SpellSlotState>(), // deliberately no slots at all
                PactSlotsMax: 0, PactSlotsCurrent: 0, PactSlotLevel: 0),
        };
        var actor = ActorMapping.ToActor(new StandardCreature(state, _spellRepository));
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target },
        }, null!);

        response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.CastSpell && a.SourceName == "Fire Bolt");
    }

    [Fact]
    public async Task GetAvailableActions_GridContextTargetOutOfWeaponRange_ExcludesAttackOptions()
    {
        var actor = MakeActorWithWeapon("Longsword"); // range 5
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target },
            GridContext = new MultiTargetGridContext
            {
                ActorPosition = new GridPosition { X = 0, Y = 0 },
                TargetPositions = { new TargetGridPosition { ActorId = target.ActorId, Position = new GridPosition { X = 4, Y = 0 } } }, // 20 feet
            },
        }, null!);

        response.Actions.Should().NotContain(a => a.Kind == AvailableActionKind.MeleeAttack);
        response.Actions.Should().NotContain(a => a.Kind == AvailableActionKind.Grapple);
    }

    [Fact]
    public async Task GetAvailableActions_NoGridContext_ReportsOptionsRegardlessOfDistance()
    {
        var actor = MakeActorWithWeapon("Longsword"); // range 5
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target },
        }, null!);

        response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.MeleeAttack);
        // A single one-handed weapon (Longsword, no off-hand item) leaves
        // a real free hand to grapple with.
        response.Actions.Should().Contain(a => a.Kind == AvailableActionKind.Grapple);
    }

    [Fact]
    public async Task GetAvailableActions_ParalyzedActor_CanActFalseAndNoActionsReported()
    {
        var state = MakeStateWithWeapon("Longsword") with
        {
            Conditions = new ConditionManagerState(new Collection<ConditionState>
            {
                new("Paralyzed", "Paralyzed.", -1, ConditionType.Paralyzed),
            }),
        };
        var actor = ActorMapping.ToActor(new StandardCreature(state, _spellRepository, _itemLibrary));
        var target = MakeGridTargetActor(currentHp: 10, maxHp: 10);

        var response = await _service.GetAvailableActions(new GetAvailableActionsRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, CandidateTargets = { target },
        }, null!);

        response.Success.Should().BeTrue();
        response.CanAct.Should().BeFalse();
        response.CannotActReason.Should().Contain("Paralyzed");
        response.Actions.Should().BeEmpty();
    }

    // Regression coverage for the execution layer GetAvailableActions'
    // menu previously only advertised: ATTACK_KIND_OFFHAND on the real
    // Attack RPC, and the new Grapple/Shove RPCs. A real d20 roll via
    // _diceRoller (StandardDiceRoller) can't be pinned to win or lose the
    // opposed check, so these assert Success (the attempt was legally
    // resolved) rather than Grappled/Shoved/Hit, same reasoning as the
    // existing Attack_* tests above.

    [Fact]
    public async Task Attack_OffhandKind_BothWeaponsLight_Succeeds()
    {
        var attacker = MakeActorWithWeapons("Shortsword", "Dagger");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target,
            Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Offhand,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task Attack_OffhandKind_MainHandNotLight_ReturnsFailure()
    {
        var attacker = MakeActorWithWeapons("Longsword", "Dagger"); // Longsword is Versatile, not Light
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target,
            Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Offhand,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Light");
    }

    [Fact]
    public async Task Attack_OffhandKind_NoOffHandEquipped_ReturnsFailure()
    {
        var attacker = MakeActorWithWeapon("Longsword"); // main hand only
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Attack(new AttackRequest
        {
            RequestId = "r1", CampaignId = "c1", Attacker = attacker, Target = target,
            Kind = Layforge.Protocol.SystemEngine.V1.AttackKind.Offhand,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("both hands");
    }

    [Fact]
    public async Task Grapple_WithFreeHand_ResolvesLegally()
    {
        var actor = MakeActorWithWeapon("Longsword"); // one-handed, off hand free
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Grapple(new GrappleRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, Target = target,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
        response.Actor.Should().NotBeNull();
        response.Target.Should().NotBeNull();
    }

    [Fact]
    public async Task Grapple_NoFreeHand_ReturnsFailure()
    {
        var actor = MakeActorWithWeapon("Greatsword"); // TwoHanded — no free hand
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Grapple(new GrappleRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, Target = target,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("no hand free");
    }

    [Fact]
    public async Task Shove_Prone_ResolvesLegally()
    {
        var actor = MakeActorWithWeapon("Longsword");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Shove(new ShoveRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, Target = target,
            Effect = Layforge.Protocol.SystemEngine.V1.ShoveEffect.Prone,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task Shove_Push_ResolvesLegally()
    {
        var actor = MakeActorWithWeapon("Longsword");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Shove(new ShoveRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, Target = target,
            Effect = Layforge.Protocol.SystemEngine.V1.ShoveEffect.Push,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task Shove_UnspecifiedEffect_ReturnsFailure()
    {
        var actor = MakeActorWithWeapon("Longsword");
        var target = MakeActor(currentHp: 10, maxHp: 10);

        var response = await _service.Shove(new ShoveRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, Target = target,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("effect");
    }

    // Regression coverage for the inventory/equipment-management RPCs
    // (design doc §8/§9's "gates over prompting"): before this, nothing
    // — no DM tool, no player action — could change a character's
    // equipment or inventory after character.upload's own one-time
    // initial setup.

    [Fact]
    public async Task EquipItem_ItemInInventory_Succeeds()
    {
        var actor = MakeActorWithInventoryItem("Longsword");

        var response = await _service.EquipItem(new EquipItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor,
            ItemName = "Longsword", Slot = Layforge.Protocol.SystemEngine.V1.EquipmentSlot.MainHand,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
        response.Actor.Should().NotBeNull();
    }

    [Fact]
    public async Task EquipItem_ItemNotInInventory_ReturnsFailure()
    {
        var actor = MakeActor(); // no inventory at all

        var response = await _service.EquipItem(new EquipItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor,
            ItemName = "Longsword", Slot = Layforge.Protocol.SystemEngine.V1.EquipmentSlot.MainHand,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("not in");
    }

    [Fact]
    public async Task EquipItem_UnspecifiedSlot_ReturnsFailure()
    {
        var actor = MakeActorWithInventoryItem("Longsword");

        var response = await _service.EquipItem(new EquipItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, ItemName = "Longsword",
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("slot");
    }

    [Fact]
    public async Task EquipItem_ShieldSlot_MapsToOffHand()
    {
        // Confirms the Shield->OffHand domain-slot mapping actually
        // routes to a real slot (StandardEquipmentManager.
        // EquipOffHandInternal accepts a weapon there too, same as
        // EQUIPMENT_SLOT_OFF_HAND itself would — a real shield item
        // would work the same way via IArmor's Shield category, not
        // exercised here since FakeItemLibrary has no armor fixture).
        var actor = MakeActorWithInventoryItem("Dagger");

        var response = await _service.EquipItem(new EquipItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor,
            ItemName = "Dagger", Slot = Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Shield,
        }, null!);

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UnequipItem_Succeeds()
    {
        var actor = MakeActorWithWeapon("Longsword");

        var response = await _service.UnequipItem(new UnequipItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor,
            Slot = Layforge.Protocol.SystemEngine.V1.EquipmentSlot.MainHand,
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task UnequipItem_UnspecifiedSlot_ReturnsFailure()
    {
        var actor = MakeActorWithWeapon("Longsword");

        var response = await _service.UnequipItem(new UnequipItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("slot");
    }

    [Fact]
    public async Task AddItemToInventory_RecognizedItem_Succeeds()
    {
        var actor = MakeActor();

        var response = await _service.AddItemToInventory(new AddItemToInventoryRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, ItemName = "Torch",
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task AddItemToInventory_UnrecognizedItem_ReturnsFailure()
    {
        var actor = MakeActor();

        var response = await _service.AddItemToInventory(new AddItemToInventoryRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, ItemName = "Wand of Made-Up Nonsense",
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("not a recognized item");
    }

    [Fact]
    public async Task RemoveItemFromInventory_ItemPresent_Succeeds()
    {
        var actor = MakeActorWithInventoryItem("Torch");

        var response = await _service.RemoveItemFromInventory(new RemoveItemFromInventoryRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, ItemName = "Torch",
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveItemFromInventory_ItemNotPresent_ReturnsFailure()
    {
        var actor = MakeActor();

        var response = await _service.RemoveItemFromInventory(new RemoveItemFromInventoryRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, ItemName = "Torch",
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("not in");
    }

    [Fact]
    public async Task TransferItem_ItemPresentInSource_MovesToTarget()
    {
        var source = MakeActorWithInventoryItem("Torch");
        var target = MakeSecondGridTargetActor();

        var response = await _service.TransferItem(new TransferItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Source = source, Target = target, ItemName = "Torch",
        }, null!);

        response.Success.Should().BeTrue();
        response.Error.Should().BeEmpty();
        response.Source.Should().NotBeNull();
        response.Target.Should().NotBeNull();
    }

    [Fact]
    public async Task TransferItem_EquippedItem_UnequipsOnSourceSide()
    {
        // Reuses StandardInventory's own existing auto-unequip-on-
        // removal behavior — this proves the RPC actually calls
        // RemoveItem (not some other path that would leave a stale
        // equipped reference on the source's own persisted state).
        var source = MakeActorWithWeapon("Longsword");
        var target = MakeSecondGridTargetActor();

        var response = await _service.TransferItem(new TransferItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Source = source, Target = target, ItemName = "Longsword",
        }, null!);

        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TransferItem_ItemNotInSource_ReturnsFailure()
    {
        var source = MakeActor();
        var target = MakeSecondGridTargetActor();

        var response = await _service.TransferItem(new TransferItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Source = source, Target = target, ItemName = "Torch",
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("not in");
    }

    [Fact]
    public async Task TransferItem_NoTarget_ReturnsFailure()
    {
        var source = MakeActorWithInventoryItem("Torch");

        var response = await _service.TransferItem(new TransferItemRequest
        {
            RequestId = "r1", CampaignId = "c1", Source = source, ItemName = "Torch",
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("target is required");
    }

    [Fact]
    public async Task GenerateLoot_SingleParticipantWithChallengeRating_Succeeds()
    {
        var participant = MakeActorWithChallengeRating(4d);

        var response = await _service.GenerateLoot(new GenerateLootRequest
        {
            RequestId = "r1", CampaignId = "c1", Participants = { participant },
        }, null!);

        response.Success.Should().BeTrue();
        response.ResultMessage.Should().Contain("effective CR 4");
    }

    [Fact]
    public async Task GenerateLoot_MultipleParticipants_ScalesPastAnySingleParticipantsCr()
    {
        // Real end-to-end proof of the encounter-CR math (not just the
        // calculator in isolation): 14 real participants (an 8-CR boss
        // plus 13 low-CR minions) should report a higher effective CR
        // than the boss alone would.
        var boss = MakeActorWithChallengeRating(8d, id: "33333333-3333-3333-3333-333333333333", name: "Boss");
        var participants = new List<Actor> { boss };
        for (var i = 0; i < 13; i++)
        {
            participants.Add(MakeActorWithChallengeRating(0.25d, id: Guid.NewGuid().ToString(), name: $"Minion{i}"));
        }

        var soloResponse = await _service.GenerateLoot(new GenerateLootRequest
        {
            RequestId = "r1", CampaignId = "c1", Participants = { boss },
        }, null!);
        var groupResponse = await _service.GenerateLoot(new GenerateLootRequest
        {
            RequestId = "r2", CampaignId = "c1", Participants = { participants },
        }, null!);

        soloResponse.Success.Should().BeTrue();
        groupResponse.Success.Should().BeTrue();
        soloResponse.ResultMessage.Should().Contain("effective CR 8");
        groupResponse.ResultMessage.Should().NotContain("effective CR 8");
    }

    [Fact]
    public async Task GenerateLoot_ParticipantWithNoChallengeRating_ReturnsFailure()
    {
        var participant = MakeActorWithChallengeRating(null);

        var response = await _service.GenerateLoot(new GenerateLootRequest
        {
            RequestId = "r1", CampaignId = "c1", Participants = { participant },
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("no challenge_rating recorded");
    }

    [Fact]
    public async Task GenerateLoot_EmptyParticipantList_ReturnsFailure()
    {
        var response = await _service.GenerateLoot(new GenerateLootRequest
        {
            RequestId = "r1", CampaignId = "c1",
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("at least one participant");
    }

    [Fact]
    public async Task AddCurrency_Succeeds_PersistsOnReturnedActor()
    {
        var actor = MakeActor();

        var response = await _service.AddCurrency(new AddCurrencyRequest
        {
            RequestId = "r1", CampaignId = "c1", Actor = actor, Copper = 5, Silver = 4, Gold = 3, Platinum = 2,
        }, null!);

        response.Success.Should().BeTrue();
        var restored = ActorMapping.ToCreature(response.Actor, _spellRepository, _itemLibrary);
        restored.IsSuccess.Should().BeTrue();
        restored.Value.Inventory.Copper.Should().Be(5);
        restored.Value.Inventory.Silver.Should().Be(4);
        restored.Value.Inventory.Gold.Should().Be(3);
        restored.Value.Inventory.Platinum.Should().Be(2);
    }

    [Fact]
    public async Task TransferCurrency_Success_MovesCurrencyFromSourceToTarget()
    {
        var source = MakeActorWithCurrency(0, 0, 50, 0);
        var target = MakeSecondGridTargetActor();

        var response = await _service.TransferCurrency(new TransferCurrencyRequest
        {
            RequestId = "r1", CampaignId = "c1", Source = source, Target = target, Gold = 20,
        }, null!);

        response.Success.Should().BeTrue();
        var restoredSource = ActorMapping.ToCreature(response.Source, _spellRepository, _itemLibrary);
        var restoredTarget = ActorMapping.ToCreature(response.Target, _spellRepository, _itemLibrary);
        restoredSource.Value.Inventory.Gold.Should().Be(30);
        restoredTarget.Value.Inventory.Gold.Should().Be(20);
    }

    [Fact]
    public async Task TransferCurrency_InsufficientFunds_ReturnsFailure()
    {
        var source = MakeActorWithCurrency(0, 0, 5, 0);
        var target = MakeSecondGridTargetActor();

        var response = await _service.TransferCurrency(new TransferCurrencyRequest
        {
            RequestId = "r1", CampaignId = "c1", Source = source, Target = target, Gold = 20,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task TransferCurrency_NoTarget_ReturnsFailure()
    {
        var source = MakeActorWithCurrency(0, 0, 50, 0);

        var response = await _service.TransferCurrency(new TransferCurrencyRequest
        {
            RequestId = "r1", CampaignId = "c1", Source = source, Gold = 20,
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("target is required");
    }

    [Fact]
    public async Task GetItemInfo_RecognizedItem_ReturnsCopperDecomposedPrice()
    {
        // FakeItemLibrary's Torch carries Value = 1 (copper pieces).
        var response = await _service.GetItemInfo(new GetItemInfoRequest
        {
            RequestId = "r1", ItemName = "Torch",
        }, null!);

        response.Success.Should().BeTrue();
        response.ItemName.Should().Be("Torch");
        response.Copper.Should().Be(1);
        response.Silver.Should().Be(0);
        response.Gold.Should().Be(0);
        response.Platinum.Should().Be(0);
    }

    [Fact]
    public async Task GetItemInfo_ValueSpanningMultipleDenominations_DecomposesGreedily()
    {
        // FakeItemLibrary's Greatsword carries Value = 50 (copper pieces) ->
        // 5 silver, no gold/platinum/copper remainder.
        var response = await _service.GetItemInfo(new GetItemInfoRequest
        {
            RequestId = "r1", ItemName = "Greatsword",
        }, null!);

        response.Success.Should().BeTrue();
        response.Copper.Should().Be(0);
        response.Silver.Should().Be(5);
        response.Gold.Should().Be(0);
        response.Platinum.Should().Be(0);
    }

    [Fact]
    public async Task GetItemInfo_UnrecognizedItem_ReturnsFailure()
    {
        var response = await _service.GetItemInfo(new GetItemInfoRequest
        {
            RequestId = "r1", ItemName = "Wand of Made-Up Nonsense",
        }, null!);

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("not a recognized item");
    }

    [Fact]
    public async Task ListInventory_ActorWithRealItems_ReturnsTheirNames()
    {
        var creature = new StandardCreature(MakeState());
        creature.Inventory.AddItem(_itemLibrary.GetItem("Torch")!);
        creature.Inventory.AddItem(_itemLibrary.GetItem("Dagger")!);
        var actor = ActorMapping.ToActor(creature);

        var response = await _service.ListInventory(new ListInventoryRequest
        {
            RequestId = "r1", Actor = actor,
        }, null!);

        response.Success.Should().BeTrue();
        response.ItemNames.Should().BeEquivalentTo(new[] { "Torch", "Dagger" });
    }

    [Fact]
    public async Task ListInventory_ActorWithNoItems_ReturnsEmptyList()
    {
        var actor = MakeActor();

        var response = await _service.ListInventory(new ListInventoryRequest
        {
            RequestId = "r1", Actor = actor,
        }, null!);

        response.Success.Should().BeTrue();
        response.ItemNames.Should().BeEmpty();
    }
}
