// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Layforge.Protocol.SystemEngine.V1;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.GrpcSidecar.Mapping;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.CharacterCreation;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Spatial;
using System.Linq;

namespace OpenCombatEngine.GrpcSidecar;

/// <summary>
/// Implements the System Engine gRPC contract (protocol/system_engine.proto)
/// against OpenCombatEngine. Every method is stateless per-call: no creature
/// state is held between calls, since Master owns campaign/character state
/// (docs/design.md §3.1, §10) — each call reconstructs a
/// <see cref="StandardCreature"/> from the Actor it was sent via
/// <see cref="ActorMapping"/> and, where relevant, serializes a full Actor
/// back out.
/// </summary>
/// <remarks>
/// Response messages that carry their own success/error field
/// (ResolveCheckResponse, ApplyEffectResponse) report failures through that
/// field. Response messages with no such field (ToJsonResponse,
/// GetCharacterStatusResponse) report failures as an
/// <see cref="RpcException"/> with <see cref="StatusCode.InvalidArgument"/>
/// instead — this is the gRPC-idiomatic error channel for a shape that
/// doesn't have its own error field. Messages with a warnings list
/// (ValidateCharacterResponse, FromJsonResponse) report failures as a
/// single "error"-severity warning rather than either.
/// </remarks>
public class SystemEngineGrpcService : SystemEngine.SystemEngineBase
{
    private readonly ISpellRepository _spellRepository;
    private readonly IDiceRoller _diceRoller;
    private readonly IItemLibrary _itemLibrary;
    private readonly OpenCombatEngine.Core.Interfaces.Loot.ILootGenerator _lootGenerator;
    private readonly OpenCombatEngine.Core.Interfaces.Loot.IEncounterChallengeCalculator _encounterCalculator;
    private readonly OpenCombatEngine.Core.Interfaces.CharacterCreation.ICharacterCreationService _characterCreationService;

    /// <summary>
    /// Constructs the service. <paramref name="spellRepository"/>,
    /// <paramref name="diceRoller"/>, <paramref name="itemLibrary"/>,
    /// <paramref name="lootGenerator"/>, <paramref name="encounterCalculator"/>,
    /// and <paramref name="characterCreationService"/> are resolved by
    /// ASP.NET Core's DI container (gRPC service instances are
    /// DI-constructed) — see <c>Program.cs</c> for where the singleton
    /// spell repository and item library instances are populated from
    /// Open5e at startup and registered.
    /// </summary>
    public SystemEngineGrpcService(
        ISpellRepository spellRepository,
        IDiceRoller diceRoller,
        IItemLibrary itemLibrary,
        OpenCombatEngine.Core.Interfaces.Loot.ILootGenerator lootGenerator,
        OpenCombatEngine.Core.Interfaces.Loot.IEncounterChallengeCalculator encounterCalculator,
        OpenCombatEngine.Core.Interfaces.CharacterCreation.ICharacterCreationService characterCreationService)
    {
        _spellRepository = spellRepository ?? throw new System.ArgumentNullException(nameof(spellRepository));
        _diceRoller = diceRoller ?? throw new System.ArgumentNullException(nameof(diceRoller));
        _itemLibrary = itemLibrary ?? throw new System.ArgumentNullException(nameof(itemLibrary));
        _lootGenerator = lootGenerator ?? throw new System.ArgumentNullException(nameof(lootGenerator));
        _encounterCalculator = encounterCalculator ?? throw new System.ArgumentNullException(nameof(encounterCalculator));
        _characterCreationService = characterCreationService ?? throw new System.ArgumentNullException(nameof(characterCreationService));
    }

    public override Task<GetCharacterSchemaResponse> GetCharacterSchema(
        GetCharacterSchemaRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetCharacterSchemaResponse
        {
            SchemaVersion = CharacterSchema.SchemaVersion,
            JsonSchema = CharacterSchema.Json,
        });
    }

    public override Task<ToJsonResponse> ToJson(ToJsonRequest request, ServerCallContext context)
    {
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (creatureResult.IsFailure)
            throw new RpcException(new Status(StatusCode.InvalidArgument, creatureResult.Error));

        return Task.FromResult(new ToJsonResponse
        {
            Json = CreatureStateJson.Serialize(creatureResult.Value.GetState()),
            SchemaVersion = ActorMapping.SchemaVersion,
        });
    }

    public override Task<FromJsonResponse> FromJson(FromJsonRequest request, ServerCallContext context)
    {
        var stateResult = CreatureStateJson.Deserialize(request.Json);
        var response = new FromJsonResponse();

        if (stateResult.IsFailure)
        {
            response.Warnings.Add(new ValidationWarning { FieldPath = "", Message = stateResult.Error, Severity = "error" });
            return Task.FromResult(response);
        }

        response.Actor = ActorMapping.ToActor(new StandardCreature(stateResult.Value, _spellRepository, _itemLibrary));
        return Task.FromResult(response);
    }

    public override Task<GetCharacterStatusResponse> GetCharacterStatus(
        GetCharacterStatusRequest request, ServerCallContext context)
    {
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (creatureResult.IsFailure)
            throw new RpcException(new Status(StatusCode.InvalidArgument, creatureResult.Error));

        return Task.FromResult(new GetCharacterStatusResponse
        {
            Status = CharacterStatusMapper.Map(creatureResult.Value.HitPoints),
        });
    }

    public override Task<StartTurnResponse> StartTurn(StartTurnRequest request, ServerCallContext context)
    {
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (creatureResult.IsFailure)
            return Task.FromResult(new StartTurnResponse { Success = false, Error = creatureResult.Error });

        var creature = creatureResult.Value;
        var turnStartResult = creature.StartTurn();

        var response = new StartTurnResponse
        {
            Success = true,
            Actor = ActorMapping.ToActor(creature),
            WokeUp = turnStartResult.WokeUp,
        };

        if (turnStartResult.DeathSaveRoll is { } roll)
        {
            response.DeathSaveRolled = true;
            var outcome = new Outcome
            {
                Total = roll.Total,
                CriticalSuccess = roll.IsCriticalSuccess,
                CriticalFailure = roll.IsCriticalFailure,
                ResultSummary = turnStartResult.WokeUp ? "woke_up" : roll.Total >= 10 ? "success" : "failure",
            };
            // Death saves are a straight 1d20, no modifier — same fact
            // ResolveCheck's own death_save handling hardcodes, safe here
            // in the sidecar (the system-engine-specific adapter) per the
            // same reasoning as that call site.
            foreach (var die in roll.IndividualRolls)
            {
                outcome.Rolls.Add(new DieRoll { Sides = 20, Result = die, Label = "d20" });
            }
            response.DeathSaveOutcome = outcome;
        }

        return Task.FromResult(response);
    }

    public override Task<ValidateCharacterResponse> ValidateCharacter(
        ValidateCharacterRequest request, ServerCallContext context)
    {
        var response = new ValidateCharacterResponse();

        string json;
        try
        {
            json = StructJson.ToJson(request.CharacterData);
        }
        catch (Google.Protobuf.InvalidJsonException ex)
        {
            response.Warnings.Add(new ValidationWarning { FieldPath = "", Message = ex.Message, Severity = "error" });
            return Task.FromResult(response);
        }

        var stateResult = CreatureStateJson.Deserialize(json);
        if (stateResult.IsFailure)
        {
            response.Warnings.Add(new ValidationWarning { FieldPath = "", Message = stateResult.Error, Severity = "error" });
        }

        return Task.FromResult(response);
    }

    public override Task<ApplyEffectResponse> ApplyEffect(ApplyEffectRequest request, ServerCallContext context)
    {
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (creatureResult.IsFailure)
            return Task.FromResult(new ApplyEffectResponse { Success = false, Error = creatureResult.Error });

        var creature = creatureResult.Value;
        var effectType = GetString(request.Effect, "effectType");

        switch (effectType)
        {
            case "damage":
            {
                var amount = (int)GetNumber(request.Effect, "amount");
                var damageTypeName = GetString(request.Effect, "damageType");
                if (damageTypeName is not null && System.Enum.TryParse<DamageType>(damageTypeName, ignoreCase: true, out var damageType))
                    creature.HitPoints.TakeDamage(amount, damageType);
                else
                    creature.HitPoints.TakeDamage(amount);
                break;
            }
            case "heal":
            {
                var amount = (int)GetNumber(request.Effect, "amount");
                creature.HitPoints.Heal(amount);
                break;
            }
            case "condition":
            {
                var name = GetString(request.Effect, "name");
                if (string.IsNullOrWhiteSpace(name))
                    return Task.FromResult(new ApplyEffectResponse { Success = false, Error = "Missing required field 'name' for effectType 'condition'." });

                var description = GetString(request.Effect, "description") ?? "";
                var durationRounds = (int)GetNumber(request.Effect, "durationRounds");
                var conditionTypeName = GetString(request.Effect, "conditionType");
                var conditionType = conditionTypeName is not null && System.Enum.TryParse<ConditionType>(conditionTypeName, ignoreCase: true, out var parsed)
                    ? parsed
                    : ConditionType.None;

                var addResult = creature.Conditions.AddCondition(new Condition(name, description, durationRounds, conditionType));
                if (addResult.IsFailure)
                    return Task.FromResult(new ApplyEffectResponse { Success = false, Error = addResult.Error });
                break;
            }
            default:
                return Task.FromResult(new ApplyEffectResponse
                {
                    Success = false,
                    Error = $"Unknown effectType '{effectType}'. Expected 'damage', 'heal', or 'condition'.",
                });
        }

        return Task.FromResult(new ApplyEffectResponse { Success = true, Actor = ActorMapping.ToActor(creature) });
    }

    public override Task<ResolveCheckResponse> ResolveCheck(ResolveCheckRequest request, ServerCallContext context)
    {
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (creatureResult.IsFailure)
            return Task.FromResult(new ResolveCheckResponse { Success = false, Error = creatureResult.Error });

        var creature = creatureResult.Value;
        var checkType = GetString(request.Params, "checkType");

        Result<DiceRollResult> rollResult;
        switch (checkType)
        {
            case "ability_check":
            {
                var abilityName = GetString(request.Params, "ability");
                if (abilityName is null || !System.Enum.TryParse<Ability>(abilityName, ignoreCase: true, out var ability))
                    return Task.FromResult(new ResolveCheckResponse { Success = false, Error = "Missing or invalid 'ability' for checkType 'ability_check'." });
                rollResult = creature.Checks.RollAbilityCheck(ability, GetString(request.Params, "skill"));
                break;
            }
            case "saving_throw":
            {
                var abilityName = GetString(request.Params, "ability");
                if (abilityName is null || !System.Enum.TryParse<Ability>(abilityName, ignoreCase: true, out var ability))
                    return Task.FromResult(new ResolveCheckResponse { Success = false, Error = "Missing or invalid 'ability' for checkType 'saving_throw'." });
                rollResult = creature.Checks.RollSavingThrow(ability);
                break;
            }
            case "death_save":
                rollResult = creature.Checks.RollDeathSave();
                break;
            default:
                return Task.FromResult(new ResolveCheckResponse
                {
                    Success = false,
                    Error = $"Unknown checkType '{checkType}'. Expected 'ability_check', 'saving_throw', or 'death_save'.",
                });
        }

        if (rollResult.IsFailure)
            return Task.FromResult(new ResolveCheckResponse { Success = false, Error = rollResult.Error });

        var roll = rollResult.Value;
        var outcome = new Outcome
        {
            Total = roll.Total,
            CriticalSuccess = roll.IsCriticalSuccess,
            CriticalFailure = roll.IsCriticalFailure,
            ResultSummary = "resolved",
        };
        // ability_check/saving_throw/death_save all roll a single d20
        // (StandardCheckManager's own "1d20+..." notation) — that's a fact
        // about this engine's checks specifically, safe to hardcode here in
        // the sidecar (the system-engine-specific adapter), unlike in
        // Master's own Go code (docs/design.md §6.1, CLAUDE.md).
        foreach (var die in roll.IndividualRolls)
        {
            outcome.Rolls.Add(new DieRoll { Sides = 20, Result = die, Label = "d20" });
        }

        return Task.FromResult(new ResolveCheckResponse { Success = true, Outcome = outcome });
    }

    public override Task<CastSpellResponse> CastSpell(CastSpellRequest request, ServerCallContext context)
    {
        var casterResult = ActorMapping.ToCreature(request.Caster, _spellRepository, _itemLibrary);
        if (casterResult.IsFailure)
            return Task.FromResult(new CastSpellResponse { Success = false, Error = casterResult.Error });
        var caster = casterResult.Value;

        var spellResult = _spellRepository.GetSpell(request.SpellName);
        if (spellResult.IsFailure)
            return Task.FromResult(new CastSpellResponse { Success = false, Error = $"Unknown spell '{request.SpellName}'." });
        var spell = spellResult.Value;

        // No target given means a self-cast — CastSpellAction requires a
        // CreatureTarget even for a self-only spell like Mage Armor
        // (there is no dedicated "self" target type in this engine).
        bool hasTarget = request.Target is not null;
        StandardCreature target = caster;
        if (request.Target is { } targetActor)
        {
            var targetResult = ActorMapping.ToCreature(targetActor, _spellRepository, _itemLibrary);
            if (targetResult.IsFailure)
                return Task.FromResult(new CastSpellResponse { Success = false, Error = targetResult.Error });
            target = targetResult.Value;
        }

        // Real range/line-of-sight gating, using positions from Master's
        // own combat map (protocol/system_engine.proto's grid_context doc
        // comment) — set only when Master actually has one for this
        // campaign with both combatants placed; a self-cast never needs
        // it (range/LOS against yourself is meaningless). Absent this,
        // context.Grid stays null below and CastSpellAction's own
        // range/LOS check (already written, already tested) simply
        // skips itself exactly as it always has.
        IGridManager? grid = null;
        if (hasTarget && request.GridContext is not null)
        {
            var gc = request.GridContext;
            var candidateGrid = new StandardGridManager();
            var casterPlaced = candidateGrid.PlaceCreature(caster, new Position(gc.CasterPosition.X, gc.CasterPosition.Y));
            var targetPlaced = candidateGrid.PlaceCreature(target, new Position(gc.TargetPosition.X, gc.TargetPosition.Y));
            if (casterPlaced.IsSuccess && targetPlaced.IsSuccess)
            {
                foreach (var obstacle in gc.Obstacles)
                {
                    candidateGrid.AddObstacle(new Position(obstacle.X, obstacle.Y));
                }
                grid = candidateGrid;
            }
            // If either placement failed (e.g. caster and target resolved
            // to the same cell — a Master-side bookkeeping inconsistency,
            // not this cast's fault), proceed without a grid rather than
            // rejecting an otherwise-valid cast over it — grid stays null,
            // same as when no grid_context is sent at all.
        }

        int? slotLevel = request.SlotLevel != 0 ? request.SlotLevel : null;
        var action = new CastSpellAction(spell, slotLevel, _diceRoller);
        var actionContext = new StandardActionContext(caster, new CreatureTarget(target), grid);

        // Captured before Execute (which mutates target.HitPoints in
        // place via CastSpellAction's own ApplySpellEffects) so Master's
        // PvP gate (design doc §9.1) has a real signal — see
        // CastSpellResponse.target_damaged's doc comment for why this is
        // computed here rather than parsing the free-text result message.
        var targetHpBefore = target.HitPoints.Current;

        var executeResult = action.Execute(actionContext);
        if (executeResult.IsFailure)
            return Task.FromResult(new CastSpellResponse { Success = false, Error = executeResult.Error });

        var response = new CastSpellResponse
        {
            Success = true,
            ResultMessage = executeResult.Value.Message,
            Caster = ActorMapping.ToActor(caster),
            TargetDamaged = target.HitPoints.Current < targetHpBefore,
        };
        if (!ReferenceEquals(target, caster))
        {
            response.Target = ActorMapping.ToActor(target);
        }
        return Task.FromResult(response);
    }

    /// <summary>
    /// Resolves a weapon attack (melee or ranged) with attacker's
    /// currently-equipped main-hand weapon — see the proto's own Attack
    /// doc comment for the real weapon-kind gate this enforces before
    /// AttackAction ever rolls. Modeled closely on <see cref="CastSpell"/>:
    /// stateless per-call, same grid-context handling, same
    /// before/after-HP TargetDamaged computation for Master's PvP gate.
    /// </summary>
    public override Task<AttackResponse> Attack(AttackRequest request, ServerCallContext context)
    {
        if (request.Kind == Layforge.Protocol.SystemEngine.V1.AttackKind.Unspecified)
            return Task.FromResult(new AttackResponse { Success = false, Error = "kind must be ATTACK_KIND_MELEE, ATTACK_KIND_RANGED, or ATTACK_KIND_OFFHAND." });

        var attackerResult = ActorMapping.ToCreature(request.Attacker, _spellRepository, _itemLibrary);
        if (attackerResult.IsFailure)
            return Task.FromResult(new AttackResponse { Success = false, Error = attackerResult.Error });
        var attacker = attackerResult.Value;

        if (request.Target is null)
            return Task.FromResult(new AttackResponse { Success = false, Error = "target is required for an attack." });
        var targetResult = ActorMapping.ToCreature(request.Target, _spellRepository, _itemLibrary);
        if (targetResult.IsFailure)
            return Task.FromResult(new AttackResponse { Success = false, Error = targetResult.Error });
        var target = targetResult.Value;

        AttackAction action;
        if (request.Kind == Layforge.Protocol.SystemEngine.V1.AttackKind.Offhand)
        {
            var mainHand = attacker.Equipment?.MainHand;
            var offHand = attacker.Equipment?.OffHand;
            if (mainHand is null || offHand is null)
                return Task.FromResult(new AttackResponse { Success = false, Error = "An off-hand attack requires a weapon equipped in both hands." });
            if (!WeaponAttackRules.IsOffhandLegal(mainHand, offHand, out var offhandReason))
                return Task.FromResult(new AttackResponse { Success = false, Error = offhandReason });
            action = WeaponAttackRules.BuildOffhandAttackAction(attacker, offHand, _diceRoller);
        }
        else
        {
            var weapon = attacker.Equipment?.MainHand;
            if (weapon is null)
                return Task.FromResult(new AttackResponse { Success = false, Error = "No weapon equipped — melee_attack/ranged_attack requires a real weapon in the attacker's main hand." });

            // The real gate: a weapon's own SRD properties, not the DM's
            // own judgment, decide whether it can be used this way — see
            // WeaponAttackRules (shared with GetAvailableActions and
            // off-hand-attack construction so they can't drift apart).
            var domainKind = request.Kind == Layforge.Protocol.SystemEngine.V1.AttackKind.Melee ? OpenCombatEngine.Core.Enums.AttackKind.Melee : OpenCombatEngine.Core.Enums.AttackKind.Ranged;
            if (!WeaponAttackRules.IsLegalFor(weapon, domainKind, out var illegalReason))
                return Task.FromResult(new AttackResponse { Success = false, Error = illegalReason });

            action = WeaponAttackRules.BuildAttackAction(attacker, weapon, domainKind, _diceRoller);
        }

        // Real range/line-of-sight gating, same reasoning and shape as
        // CastSpell's own grid_context handling — set only when Master
        // actually has a combat map for this campaign with both
        // combatants placed; absent this, context.Grid stays null and
        // AttackAction's own range/LOS check (already written, already
        // tested) simply skips itself.
        IGridManager? grid = null;
        if (request.GridContext is not null)
        {
            var gc = request.GridContext;
            var candidateGrid = new StandardGridManager();
            var attackerPlaced = candidateGrid.PlaceCreature(attacker, new Position(gc.CasterPosition.X, gc.CasterPosition.Y));
            var targetPlaced = candidateGrid.PlaceCreature(target, new Position(gc.TargetPosition.X, gc.TargetPosition.Y));
            if (attackerPlaced.IsSuccess && targetPlaced.IsSuccess)
            {
                foreach (var obstacle in gc.Obstacles)
                {
                    candidateGrid.AddObstacle(new Position(obstacle.X, obstacle.Y));
                }
                grid = candidateGrid;
            }
        }

        var actionContext = new StandardActionContext(attacker, new CreatureTarget(target), grid);

        // Captured before Execute (which mutates target.HitPoints in place
        // via StandardCreature.ResolveAttack) so Master's PvP gate (design
        // doc §9.1) has a real signal — same reasoning as CastSpell's own
        // targetHpBefore capture.
        var targetHpBefore = target.HitPoints.Current;

        var executeResult = action.Execute(actionContext);
        if (executeResult.IsFailure)
            return Task.FromResult(new AttackResponse { Success = false, Error = executeResult.Error });

        return Task.FromResult(new AttackResponse
        {
            Success = true,
            Hit = executeResult.Value.Success,
            ResultMessage = executeResult.Value.Message,
            Attacker = ActorMapping.ToActor(attacker),
            Target = ActorMapping.ToActor(target),
            TargetDamaged = target.HitPoints.Current < targetHpBefore,
        });
    }

    /// <summary>
    /// Resolves a real SRD grapple attempt — see the proto's own Grapple
    /// doc comment. Stateless per-call and grid-context-handled the same
    /// way as Attack/CastSpell.
    /// </summary>
    public override Task<GrappleResponse> Grapple(GrappleRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new GrappleResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        if (request.Target is null)
            return Task.FromResult(new GrappleResponse { Success = false, Error = "target is required for a grapple attempt." });
        var targetResult = ActorMapping.ToCreature(request.Target, _spellRepository, _itemLibrary);
        if (targetResult.IsFailure)
            return Task.FromResult(new GrappleResponse { Success = false, Error = targetResult.Error });
        var target = targetResult.Value;

        IGridManager? grid = BuildGridFromContext(request.GridContext, actor, target);

        var action = new GrappleAction(_diceRoller);
        var actionContext = new StandardActionContext(actor, new CreatureTarget(target), grid);

        var executeResult = action.Execute(actionContext);
        if (executeResult.IsFailure)
            return Task.FromResult(new GrappleResponse { Success = false, Error = executeResult.Error });

        return Task.FromResult(new GrappleResponse
        {
            Success = true,
            Grappled = executeResult.Value.Success,
            ResultMessage = executeResult.Value.Message,
            Actor = ActorMapping.ToActor(actor),
            Target = ActorMapping.ToActor(target),
        });
    }

    /// <summary>
    /// Resolves a real SRD shove attempt (prone or push, per
    /// request.effect) — see the proto's own Shove doc comment.
    /// Stateless per-call and grid-context-handled the same way as
    /// Attack/CastSpell.
    /// </summary>
    public override Task<ShoveResponse> Shove(ShoveRequest request, ServerCallContext context)
    {
        if (request.Effect == Layforge.Protocol.SystemEngine.V1.ShoveEffect.Unspecified)
            return Task.FromResult(new ShoveResponse { Success = false, Error = "effect must be SHOVE_EFFECT_PRONE or SHOVE_EFFECT_PUSH." });

        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new ShoveResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        if (request.Target is null)
            return Task.FromResult(new ShoveResponse { Success = false, Error = "target is required for a shove attempt." });
        var targetResult = ActorMapping.ToCreature(request.Target, _spellRepository, _itemLibrary);
        if (targetResult.IsFailure)
            return Task.FromResult(new ShoveResponse { Success = false, Error = targetResult.Error });
        var target = targetResult.Value;

        IGridManager? grid = BuildGridFromContext(request.GridContext, actor, target);

        var domainEffect = request.Effect == Layforge.Protocol.SystemEngine.V1.ShoveEffect.Prone
            ? OpenCombatEngine.Core.Enums.ShoveEffect.Prone
            : OpenCombatEngine.Core.Enums.ShoveEffect.Push;
        var action = new ShoveAction(_diceRoller, domainEffect);
        var actionContext = new StandardActionContext(actor, new CreatureTarget(target), grid);

        var executeResult = action.Execute(actionContext);
        if (executeResult.IsFailure)
            return Task.FromResult(new ShoveResponse { Success = false, Error = executeResult.Error });

        return Task.FromResult(new ShoveResponse
        {
            Success = true,
            Shoved = executeResult.Value.Success,
            ResultMessage = executeResult.Value.Message,
            Actor = ActorMapping.ToActor(actor),
            Target = ActorMapping.ToActor(target),
        });
    }

    /// <summary>
    /// Builds a throwaway <see cref="StandardGridManager"/> from a
    /// single-target <see cref="GridContext"/> the same way Attack/
    /// CastSpell each already do inline — shared here since Grapple and
    /// Shove need the identical construction. Returns null (skip the
    /// range/LOS check) when gridContext is omitted or either placement
    /// fails.
    /// </summary>
    private static IGridManager? BuildGridFromContext(GridContext? gridContext, StandardCreature actor, StandardCreature target)
    {
        if (gridContext is null) return null;
        var candidateGrid = new StandardGridManager();
        var actorPlaced = candidateGrid.PlaceCreature(actor, new Position(gridContext.CasterPosition.X, gridContext.CasterPosition.Y));
        var targetPlaced = candidateGrid.PlaceCreature(target, new Position(gridContext.TargetPosition.X, gridContext.TargetPosition.Y));
        if (!actorPlaced.IsSuccess || !targetPlaced.IsSuccess) return null;
        foreach (var obstacle in gridContext.Obstacles)
        {
            candidateGrid.AddObstacle(new Position(obstacle.X, obstacle.Y));
        }
        return candidateGrid;
    }

    /// <summary>
    /// Maps the proto EquipmentSlot to OpenCombatEngine's own domain
    /// enum — false for ATTACK_KIND_UNSPECIFIED-style Unspecified (a
    /// real rejection, never guessed at). EQUIPMENT_SLOT_SHIELD maps to
    /// the same domain OffHand slot as EQUIPMENT_SLOT_OFF_HAND: the
    /// domain model has no separate Shield slot — Equipment.Shield vs.
    /// Equipment.OffHand is decided internally by
    /// StandardEquipmentManager.EquipOffHandInternal based on whether
    /// the item passed in is a shield or a weapon. The proto keeps
    /// Shield as its own value purely for a clearer DM-facing tool
    /// argument ("shield" reads better than "off_hand" for donning one).
    /// </summary>
    private static bool TryMapEquipmentSlot(Layforge.Protocol.SystemEngine.V1.EquipmentSlot protoSlot, out OpenCombatEngine.Core.Enums.EquipmentSlot domainSlot)
    {
        switch (protoSlot)
        {
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.MainHand: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.OffHand: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Armor: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Armor; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Shield: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Head: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Head; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Neck: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Neck; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Shoulders: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Shoulders; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Hands: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Hands; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Waist: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Waist; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Feet: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Feet; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Ring1: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Ring1; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Ring2: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Ring2; return true;
            case Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Back: domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.Back; return true;
            default:
                domainSlot = OpenCombatEngine.Core.Enums.EquipmentSlot.None;
                return false;
        }
    }

    /// <summary>
    /// The reverse of <see cref="TryMapEquipmentSlot"/> — used by
    /// <see cref="ListCarriedItems"/> to report which slot an equipped
    /// item occupies. There is no domain "Shield" slot (a shield lives in
    /// the same domain OffHand slot as an off-hand weapon — see
    /// <c>StandardEquipmentManager.EquipOffHandInternal</c>), so this
    /// always maps OffHand back to the proto OffHand value; a caller that
    /// wants "shield" specifically for display purposes checks
    /// <c>IEquipmentManager.Shield</c> itself (see <see cref="DescribeLocation"/>).
    /// </summary>
    private static Layforge.Protocol.SystemEngine.V1.EquipmentSlot ToProtoEquipmentSlot(OpenCombatEngine.Core.Enums.EquipmentSlot domainSlot)
    {
        return domainSlot switch
        {
            OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.MainHand,
            OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.OffHand,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Armor => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Armor,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Head => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Head,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Neck => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Neck,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Shoulders => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Shoulders,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Hands => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Hands,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Waist => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Waist,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Feet => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Feet,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Ring1 => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Ring1,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Ring2 => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Ring2,
            OpenCombatEngine.Core.Enums.EquipmentSlot.Back => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Back,
            _ => Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Unspecified,
        };
    }

    /// <summary>
    /// Reports whether moving itemToStow inside destination would create a
    /// containment cycle — true when destination IS itemToStow, or is
    /// reachable by walking itemToStow's own nested Contents (only
    /// possible when itemToStow is itself a container). <see cref="ContainerItem.AddItem"/>
    /// only guards the direct case (an item cannot contain itself); this
    /// closes the indirect one (a Pack cannot be stowed inside a Pouch
    /// that's already inside that same Pack).
    /// </summary>
    private static bool WouldCreateCycle(IItem itemToStow, IContainer destination)
    {
        if (ReferenceEquals(itemToStow, destination)) return true;
        if (itemToStow is not IContainer itemAsContainer) return false;
        foreach (var nested in itemAsContainer.Contents)
        {
            if (WouldCreateCycle(nested, destination)) return true;
        }
        return false;
    }

    /// <summary>
    /// Renders one <see cref="OpenCombatEngine.Core.Models.Creatures.CarriedItemLocation"/>
    /// into ListCarriedItems' wire shape — the single place that decides
    /// what a location actually reads like ("wielded (main hand)",
    /// "stowed in Explorer's Pack", "quick access"), so <see cref="StowItem"/>/
    /// <see cref="DrawItem"/>'s own result_message text and this RPC's
    /// output never drift apart.
    /// </summary>
    private static (string Description, Layforge.Protocol.SystemEngine.V1.CarryLocationKind Kind, string ContainerName, Layforge.Protocol.SystemEngine.V1.EquipmentSlot Slot)
        DescribeLocation(OpenCombatEngine.Core.Interfaces.Creatures.ICreature actor, OpenCombatEngine.Core.Models.Creatures.CarriedItemLocation location)
    {
        if (location.EquippedSlot is OpenCombatEngine.Core.Enums.EquipmentSlot slot)
        {
            string label = slot switch
            {
                OpenCombatEngine.Core.Enums.EquipmentSlot.MainHand => "wielded (main hand)",
                OpenCombatEngine.Core.Enums.EquipmentSlot.OffHand =>
                    ReferenceEquals(actor.Equipment.Shield, location.Item) ? "wielded (shield)" : "wielded (off hand)",
                OpenCombatEngine.Core.Enums.EquipmentSlot.Back => "worn (back)",
                _ => $"worn ({slot.ToString().ToLowerInvariant()})",
            };
            return (label, Layforge.Protocol.SystemEngine.V1.CarryLocationKind.Equipped, string.Empty, ToProtoEquipmentSlot(slot));
        }
        if (location.ParentContainer is IContainer container)
        {
            return ($"stowed in {container.Name}", Layforge.Protocol.SystemEngine.V1.CarryLocationKind.Stowed, container.Name, Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Unspecified);
        }
        return ("quick access", Layforge.Protocol.SystemEngine.V1.CarryLocationKind.QuickAccess, string.Empty, Layforge.Protocol.SystemEngine.V1.EquipmentSlot.Unspecified);
    }

    /// <summary>
    /// Moves an item already in actor's inventory into an equipment
    /// slot — see the proto's own EquipItem doc comment. Notably,
    /// <c>StandardEquipmentManager.Equip</c> itself does not verify the
    /// item is actually carried (a documented loose-coupling choice,
    /// ADR 0016), so this handler does that check itself before ever
    /// calling it — the real gate this RPC exists to provide.
    /// </summary>
    public override Task<EquipItemResponse> EquipItem(EquipItemRequest request, ServerCallContext context)
    {
        if (!TryMapEquipmentSlot(request.Slot, out var domainSlot))
            return Task.FromResult(new EquipItemResponse { Success = false, Error = "slot must be a real equipment slot." });

        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new EquipItemResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var item = actor.Inventory.GetItem(request.ItemName);
        if (item is null)
            return Task.FromResult(new EquipItemResponse { Success = false, Error = $"{request.ItemName} is not in {actor.Name}'s inventory." });

        var equipResult = actor.Equipment.Equip(item, domainSlot);
        if (equipResult.IsFailure)
            return Task.FromResult(new EquipItemResponse { Success = false, Error = equipResult.Error });

        return Task.FromResult(new EquipItemResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} equips {item.Name}.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Clears one equipment slot — the item stays in actor's inventory
    /// (this only changes what's readied, not what's carried).
    /// </summary>
    public override Task<UnequipItemResponse> UnequipItem(UnequipItemRequest request, ServerCallContext context)
    {
        if (!TryMapEquipmentSlot(request.Slot, out var domainSlot))
            return Task.FromResult(new UnequipItemResponse { Success = false, Error = "slot must be a real equipment slot." });

        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new UnequipItemResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var unequipResult = actor.Equipment.Unequip(domainSlot);
        if (unequipResult.IsFailure)
            return Task.FromResult(new UnequipItemResponse { Success = false, Error = unequipResult.Error });

        return Task.FromResult(new UnequipItemResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} unequips {domainSlot}.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Resolves item_name against the sidecar's real Open5e-backed
    /// item library and adds it to actor's inventory — see the proto's
    /// own AddItemToInventory doc comment for the real gate this
    /// enforces (an unrecognized name is a rejection, never invented).
    /// </summary>
    public override Task<AddItemToInventoryResponse> AddItemToInventory(AddItemToInventoryRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new AddItemToInventoryResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var item = _itemLibrary.GetItem(request.ItemName);
        if (item is null)
            return Task.FromResult(new AddItemToInventoryResponse { Success = false, Error = $"'{request.ItemName}' is not a recognized item." });

        var addResult = actor.Inventory.AddItem(item);
        if (addResult.IsFailure)
            return Task.FromResult(new AddItemToInventoryResponse { Success = false, Error = addResult.Error });

        return Task.FromResult(new AddItemToInventoryResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} receives {item.Name}.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Removes a real member of actor's inventory permanently
    /// (discarded — this contract has no "item on the ground" concept
    /// yet). Auto-unequips first if the item was equipped —
    /// <see cref="OpenCombatEngine.Implementation.Items.StandardInventory.RemoveItem"/>'s
    /// own existing behavior, not duplicated here.
    /// </summary>
    public override Task<RemoveItemFromInventoryResponse> RemoveItemFromInventory(RemoveItemFromInventoryRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new RemoveItemFromInventoryResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var item = actor.Inventory.GetItem(request.ItemName);
        if (item is null)
            return Task.FromResult(new RemoveItemFromInventoryResponse { Success = false, Error = $"{request.ItemName} is not in {actor.Name}'s inventory." });

        var removeResult = actor.Inventory.RemoveItem(item);
        if (removeResult.IsFailure)
            return Task.FromResult(new RemoveItemFromInventoryResponse { Success = false, Error = removeResult.Error });

        return Task.FromResult(new RemoveItemFromInventoryResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} discards {item.Name}.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Moves a real item source is carrying into target's inventory —
    /// see the proto's own TransferItem doc comment. Explicitly ends
    /// attunement first if the item is currently attuned (SRD: giving
    /// away an attuned item ends attunement) — <c>StandardInventory.
    /// RemoveItem</c>'s own auto-unequip does not also do this, so it's
    /// handled here rather than left silently inconsistent.
    /// </summary>
    /// <remarks>
    /// Looked up via <see cref="OpenCombatEngine.Core.Interfaces.Creatures.ICreature.GetCarriedItemLocations"/>
    /// (the same traversal StowItem/DrawItem/ListCarriedItems all share),
    /// not <c>Inventory.GetItem</c> — closes a real, previously-flagged
    /// gap: <c>GetItem</c> only ever searched the flat top-level list, so
    /// an item stowed inside a container (looting a corpse's own
    /// backpack, say) could never be found or transferred at all, only
    /// something already equipped or quick-access. The removed item
    /// always lands in target's flat inventory as quick-access — never
    /// re-nested inside one of target's own containers — the same
    /// "surfaces to quick-access" behavior DrawItem already documents
    /// for bringing a stowed item back into hand. If item_name is itself
    /// a container, its own nested Contents move with it intact (they
    /// live inside that same object, not a separate structure this
    /// method has to reconstruct).
    /// </remarks>
    public override Task<TransferItemResponse> TransferItem(TransferItemRequest request, ServerCallContext context)
    {
        var sourceResult = ActorMapping.ToCreature(request.Source, _spellRepository, _itemLibrary);
        if (sourceResult.IsFailure)
            return Task.FromResult(new TransferItemResponse { Success = false, Error = sourceResult.Error });
        var source = sourceResult.Value;

        if (request.Target is null)
            return Task.FromResult(new TransferItemResponse { Success = false, Error = "target is required for a transfer." });
        var targetResult = ActorMapping.ToCreature(request.Target, _spellRepository, _itemLibrary);
        if (targetResult.IsFailure)
            return Task.FromResult(new TransferItemResponse { Success = false, Error = targetResult.Error });
        var target = targetResult.Value;

        var itemLocation = source.GetCarriedItemLocations().FirstOrDefault(l => l.Item.Name == request.ItemName);
        if (itemLocation is null)
            return Task.FromResult(new TransferItemResponse { Success = false, Error = $"{request.ItemName} is not in {source.Name}'s inventory." });
        var item = itemLocation.Item;

        if (item is IMagicItem magicItem && source.Equipment.AttunedItems.Contains(magicItem))
        {
            source.Equipment.UnattuneItem(magicItem);
        }

        // A nested item is never itself a member of the flat inventory
        // list (see StandardCreature.GetCarriedItemLocations' own
        // remarks), so Inventory.RemoveItem would fail to find it —
        // remove it from its actual parent container instead. Anything
        // else (equipped or already quick-access) IS a flat member, same
        // as before this fix — Inventory.RemoveItem's own auto-unequip
        // still applies unchanged for the equipped case.
        var removeResult = itemLocation.ParentContainer is IContainer parentContainer
            ? parentContainer.RemoveItem(item)
            : source.Inventory.RemoveItem(item);
        if (removeResult.IsFailure)
            return Task.FromResult(new TransferItemResponse { Success = false, Error = removeResult.Error });

        var addResult = target.Inventory.AddItem(item);
        if (addResult.IsFailure)
            return Task.FromResult(new TransferItemResponse { Success = false, Error = addResult.Error });

        return Task.FromResult(new TransferItemResponse
        {
            Success = true,
            ResultMessage = $"{source.Name} gives {item.Name} to {target.Name}.",
            Source = ActorMapping.ToActor(source),
            Target = ActorMapping.ToActor(target),
        });
    }

    /// <summary>
    /// Moves a real carried item (equipped or quick-access) into a named
    /// container actor is also carrying — see the proto's own StowItem
    /// doc comment for the real cost (always the Action) and rejections
    /// (cycle, capacity, no Action left). Looked up via
    /// <see cref="OpenCombatEngine.Core.Interfaces.Creatures.ICreature.GetCarriedItemLocations"/>
    /// rather than <c>Inventory.GetItem</c>, since the destination
    /// container (and, for a re-stow, the item itself) may already be
    /// nested rather than a top-level inventory member.
    /// </summary>
    public override Task<StowItemResponse> StowItem(StowItemRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new StowItemResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var locations = actor.GetCarriedItemLocations();
        var itemLocation = locations.FirstOrDefault(l => l.Item.Name == request.ItemName);
        if (itemLocation is null)
            return Task.FromResult(new StowItemResponse { Success = false, Error = $"{request.ItemName} is not in {actor.Name}'s inventory." });

        var container = locations.FirstOrDefault(l => l.Item.Name == request.ContainerName)?.Item as IContainer;
        if (container is null)
            return Task.FromResult(new StowItemResponse { Success = false, Error = $"{request.ContainerName} is not a real container {actor.Name} is carrying." });

        if (ReferenceEquals(itemLocation.ParentContainer, container))
            return Task.FromResult(new StowItemResponse { Success = false, Error = $"{request.ItemName} is already in {request.ContainerName}." });

        if (WouldCreateCycle(itemLocation.Item, container))
            return Task.FromResult(new StowItemResponse { Success = false, Error = $"Cannot stow {request.ItemName} inside {request.ContainerName} — {request.ContainerName} is already inside {request.ItemName}." });

        if (!actor.ActionEconomy.HasAction)
            return Task.FromResult(new StowItemResponse { Success = false, Error = $"{actor.Name} has no Action remaining this turn." });

        // Add to the destination before removing from wherever it was —
        // a capacity rejection here must never leave the item vanished.
        var addResult = container.AddItem(itemLocation.Item);
        if (addResult.IsFailure)
            return Task.FromResult(new StowItemResponse { Success = false, Error = addResult.Error });

        if (itemLocation.ParentContainer is IContainer oldParent)
        {
            oldParent.RemoveItem(itemLocation.Item);
        }
        else
        {
            // Was equipped or flat/quick-access — RemoveItem auto-unequips
            // first if needed (StandardInventory's own existing behavior).
            actor.Inventory.RemoveItem(itemLocation.Item);
        }

        actor.ActionEconomy.UseAction();

        return Task.FromResult(new StowItemResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} stows {itemLocation.Item.Name} in {container.Name}.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Brings a carried item to hand/ready-to-use — see the proto's own
    /// DrawItem doc comment for the two different costs this enforces
    /// depending on where the item actually is. A stowed item surfaces
    /// into the flat inventory (quick access), not straight into an
    /// equipment slot — a separate EquipItem call readies it from there,
    /// the same "equip requires an already-carried, top-level item"
    /// constraint EquipItem already enforces.
    /// </summary>
    public override Task<DrawItemResponse> DrawItem(DrawItemRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new DrawItemResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var itemLocation = actor.GetCarriedItemLocations().FirstOrDefault(l => l.Item.Name == request.ItemName);
        if (itemLocation is null)
            return Task.FromResult(new DrawItemResponse { Success = false, Error = $"{request.ItemName} is not in {actor.Name}'s inventory." });

        if (itemLocation.EquippedSlot is not null)
            return Task.FromResult(new DrawItemResponse { Success = false, Error = $"{request.ItemName} is already in hand." });

        if (itemLocation.ParentContainer is IContainer parent)
        {
            if (!actor.ActionEconomy.HasAction)
                return Task.FromResult(new DrawItemResponse { Success = false, Error = $"{actor.Name} has no Action remaining this turn." });

            parent.RemoveItem(itemLocation.Item);
            actor.Inventory.AddItem(itemLocation.Item);
            actor.ActionEconomy.UseAction();
        }
        else
        {
            // Quick access already — bringing it fully to hand only costs
            // this turn's free object interaction, never falls back to the
            // Action if that's already spent.
            if (!actor.ActionEconomy.TryUseFreeObjectInteraction())
                return Task.FromResult(new DrawItemResponse { Success = false, Error = $"{actor.Name} has already used this turn's free object interaction." });
        }

        return Task.FromResult(new DrawItemResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} draws {itemLocation.Item.Name}.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Computes CR-appropriate treasure for a roster of creatures — see
    /// the proto's own GenerateLoot doc comment for why this runs at
    /// encounter-prep time rather than as a post-combat reward, and why
    /// combining multiple participants' CRs is real system-engine math
    /// rather than something Master computes. Every participant must
    /// resolve to a real creature with a real challenge_rating recorded;
    /// an empty list or any missing CR is a real rejection, never an
    /// invented default (CLAUDE.md's "gates over prompting").
    /// </summary>
    public override Task<GenerateLootResponse> GenerateLoot(GenerateLootRequest request, ServerCallContext context)
    {
        if (request.Participants.Count == 0)
            return Task.FromResult(new GenerateLootResponse { Success = false, Error = "at least one participant is required." });

        var challengeRatings = new System.Collections.Generic.List<double>();
        foreach (var participantActor in request.Participants)
        {
            var participantResult = ActorMapping.ToCreature(participantActor, _spellRepository, _itemLibrary);
            if (participantResult.IsFailure)
                return Task.FromResult(new GenerateLootResponse { Success = false, Error = participantResult.Error });

            var participant = participantResult.Value;
            if (participant.ChallengeRating is not double cr || cr < 0)
                return Task.FromResult(new GenerateLootResponse { Success = false, Error = $"{participant.Name} has no challenge_rating recorded." });

            challengeRatings.Add(cr);
        }

        var effectiveCr = _encounterCalculator.CalculateEffectiveChallengeRating(challengeRatings);
        var bundle = _lootGenerator.GenerateLoot((int)effectiveCr);

        return Task.FromResult(new GenerateLootResponse
        {
            Success = true,
            Copper = bundle.Copper,
            Silver = bundle.Silver,
            Gold = bundle.Gold,
            Platinum = bundle.Platinum,
            ItemName = bundle.Items.Count > 0 ? bundle.Items[0].Name : string.Empty,
            ResultMessage = $"Generated loot for {request.Participants.Count} participant(s) at effective CR {effectiveCr}.",
        });
    }

    /// <summary>
    /// Adds currency to actor's inventory from nothing — the currency
    /// equivalent of AddItemToInventory.
    /// </summary>
    public override Task<AddCurrencyResponse> AddCurrency(AddCurrencyRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new AddCurrencyResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var addResult = actor.Inventory.AddCurrency(request.Copper, request.Silver, request.Gold, request.Platinum);
        if (addResult.IsFailure)
            return Task.FromResult(new AddCurrencyResponse { Success = false, Error = addResult.Error });

        return Task.FromResult(new AddCurrencyResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} receives {request.Copper}cp, {request.Silver}sp, {request.Gold}gp, {request.Platinum}pp.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Moves currency from source's inventory into target's — the
    /// currency equivalent of TransferItem. Real rejection if source
    /// doesn't carry enough of a requested denomination (see
    /// <see cref="OpenCombatEngine.Implementation.Items.StandardInventory.RemoveCurrency"/>
    /// — this does not make change across denominations).
    /// </summary>
    public override Task<TransferCurrencyResponse> TransferCurrency(TransferCurrencyRequest request, ServerCallContext context)
    {
        var sourceResult = ActorMapping.ToCreature(request.Source, _spellRepository, _itemLibrary);
        if (sourceResult.IsFailure)
            return Task.FromResult(new TransferCurrencyResponse { Success = false, Error = sourceResult.Error });
        var source = sourceResult.Value;

        if (request.Target is null)
            return Task.FromResult(new TransferCurrencyResponse { Success = false, Error = "target is required for a transfer." });
        var targetResult = ActorMapping.ToCreature(request.Target, _spellRepository, _itemLibrary);
        if (targetResult.IsFailure)
            return Task.FromResult(new TransferCurrencyResponse { Success = false, Error = targetResult.Error });
        var target = targetResult.Value;

        var removeResult = source.Inventory.RemoveCurrency(request.Copper, request.Silver, request.Gold, request.Platinum);
        if (removeResult.IsFailure)
            return Task.FromResult(new TransferCurrencyResponse { Success = false, Error = removeResult.Error });

        var addResult = target.Inventory.AddCurrency(request.Copper, request.Silver, request.Gold, request.Platinum);
        if (addResult.IsFailure)
            return Task.FromResult(new TransferCurrencyResponse { Success = false, Error = addResult.Error });

        return Task.FromResult(new TransferCurrencyResponse
        {
            Success = true,
            ResultMessage = $"{source.Name} gives {request.Copper}cp, {request.Silver}sp, {request.Gold}gp, {request.Platinum}pp to {target.Name}.",
            Source = ActorMapping.ToActor(source),
            Target = ActorMapping.ToActor(target),
        });
    }

    /// <summary>
    /// Removes currency from actor's inventory into nothing —
    /// AddCurrency's inverse, and the single-actor equivalent of
    /// TransferCurrency's own remove-half. Real rejection if actor
    /// doesn't carry enough of a requested denomination.
    /// </summary>
    public override Task<RemoveCurrencyResponse> RemoveCurrency(RemoveCurrencyRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new RemoveCurrencyResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var removeResult = actor.Inventory.RemoveCurrency(request.Copper, request.Silver, request.Gold, request.Platinum);
        if (removeResult.IsFailure)
            return Task.FromResult(new RemoveCurrencyResponse { Success = false, Error = removeResult.Error });

        return Task.FromResult(new RemoveCurrencyResponse
        {
            Success = true,
            ResultMessage = $"{actor.Name} loses {request.Copper}cp, {request.Silver}sp, {request.Gold}gp, {request.Platinum}pp.",
            Actor = ActorMapping.ToActor(actor),
        });
    }

    /// <summary>
    /// Looks up item_name's real base price (<see cref="IItem.Value"/>,
    /// copper pieces) from <see cref="_itemLibrary"/> — the same library
    /// AddItemToInventory resolves names against — without adding it to
    /// anyone's inventory. Decomposes the value into the fewest coins
    /// (platinum first, down to copper) so the caller can hand the result
    /// straight to TransferCurrency.
    /// </summary>
    public override Task<GetItemInfoResponse> GetItemInfo(GetItemInfoRequest request, ServerCallContext context)
    {
        var item = _itemLibrary.GetItem(request.ItemName);
        if (item is null)
            return Task.FromResult(new GetItemInfoResponse { Success = false, Error = $"'{request.ItemName}' is not a recognized item." });

        var remaining = item.Value;
        var platinum = remaining / 1000; remaining %= 1000;
        var gold = remaining / 100; remaining %= 100;
        var silver = remaining / 10; remaining %= 10;
        var copper = remaining;

        return Task.FromResult(new GetItemInfoResponse
        {
            Success = true,
            ItemName = item.Name,
            Copper = copper,
            Silver = silver,
            Gold = gold,
            Platinum = platinum,
            ResultMessage = $"{item.Name}: {platinum}pp, {gold}gp, {silver}sp, {copper}cp.",
        });
    }

    /// <summary>
    /// Returns the real item names currently held by actor. Exists because
    /// Actor.character_data is opaque to Master (see the proto's own doc
    /// comment on that field) — a vendor-stock listing needs this real RPC
    /// rather than Master parsing character_data's inventory shape itself.
    /// </summary>
    public override Task<ListInventoryResponse> ListInventory(ListInventoryRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new ListInventoryResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var response = new ListInventoryResponse { Success = true };
        response.ItemNames.AddRange(actor.Inventory.Items.Select(i => i.Name));
        return Task.FromResult(response);
    }

    /// <summary>
    /// Answers "where is everything actor is carrying" — see the proto's
    /// own ListCarriedItems doc comment. Every entry is computed fresh
    /// from <see cref="OpenCombatEngine.Core.Interfaces.Creatures.ICreature.GetCarriedItemLocations"/>
    /// via <see cref="DescribeLocation"/>, never a separately-tracked
    /// field.
    /// </summary>
    public override Task<ListCarriedItemsResponse> ListCarriedItems(ListCarriedItemsRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new ListCarriedItemsResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var response = new ListCarriedItemsResponse { Success = true };
        foreach (var location in actor.GetCarriedItemLocations())
        {
            var (description, kind, containerName, slot) = DescribeLocation(actor, location);
            response.Items.Add(new CarriedItem
            {
                ItemName = location.Item.Name,
                LocationDescription = description,
                Kind = kind,
                ContainerName = containerName,
                Slot = slot,
            });
        }
        return Task.FromResult(response);
    }

    /// <summary>
    /// Computes the full concrete list of mechanically legal actions
    /// actor could take right now, against each of candidate_targets —
    /// see the proto's own doc comment for why this exists (real
    /// engine-computed data instead of the DM model guessing). Every
    /// fact reported here is independently re-enforced by Attack/
    /// CastSpell when actually called — this is a menu, not a new
    /// source of authority, so a race between this call and a
    /// subsequent state change (another creature's turn happens first)
    /// is not a real gate weakness, only a stale suggestion.
    /// </summary>
    public override Task<GetAvailableActionsResponse> GetAvailableActions(GetAvailableActionsRequest request, ServerCallContext context)
    {
        var actorResult = ActorMapping.ToCreature(request.Actor, _spellRepository, _itemLibrary);
        if (actorResult.IsFailure)
            return Task.FromResult(new GetAvailableActionsResponse { Success = false, Error = actorResult.Error });
        var actor = actorResult.Value;

        var response = new GetAvailableActionsResponse
        {
            Success = true,
            HasAction = actor.ActionEconomy?.HasAction ?? true,
            HasBonusAction = actor.ActionEconomy?.HasBonusAction ?? true,
            HasReaction = actor.ActionEconomy?.HasReaction ?? true,
        };

        // The real gate this whole RPC exists to surface without a
        // wasted round trip: an Incapacitated/Paralyzed/Stunned/
        // Petrified actor has no legal actions at all — see
        // IncapacitationCheck's own remarks for why this is deliberately
        // independent of the HP-based Unconscious/Dying/Dead status.
        if (IncapacitationCheck.BlockingCondition(actor) is { } blockingCondition)
        {
            response.CanAct = false;
            response.CannotActReason = blockingCondition;
            return Task.FromResult(response);
        }
        response.CanAct = true;

        // Resolve every candidate target actor could plausibly act
        // against — unresolvable entries are skipped rather than
        // failing the whole call (a stale/malformed candidate shouldn't
        // hide every other real option).
        var targets = new System.Collections.Generic.List<(string ActorId, StandardCreature Creature)>();
        foreach (var candidateActor in request.CandidateTargets)
        {
            var candidateResult = ActorMapping.ToCreature(candidateActor, _spellRepository, _itemLibrary);
            if (candidateResult.IsSuccess)
            {
                targets.Add((candidateActor.ActorId, candidateResult.Value));
            }
        }

        // Real range/line-of-sight narrowing, same reasoning and shape
        // as CastSpell/Attack's own grid_context handling — set only
        // when Master actually has a combat map with actor and (some
        // or all) candidate_targets placed; absent this, grid stays
        // null and every option below is reported unfiltered.
        IGridManager? grid = null;
        if (request.GridContext is not null)
        {
            var gc = request.GridContext;
            var candidateGrid = new StandardGridManager();
            var actorPlaced = candidateGrid.PlaceCreature(actor, new Position(gc.ActorPosition.X, gc.ActorPosition.Y));
            bool allPlaced = actorPlaced.IsSuccess;
            foreach (var targetPosition in gc.TargetPositions)
            {
                var matchingTarget = targets.Find(t => t.ActorId == targetPosition.ActorId);
                if (matchingTarget.Creature is null) continue;
                var placed = candidateGrid.PlaceCreature(matchingTarget.Creature, new Position(targetPosition.Position.X, targetPosition.Position.Y));
                if (!placed.IsSuccess) allPlaced = false;
            }
            if (allPlaced)
            {
                foreach (var obstacle in gc.Obstacles)
                {
                    candidateGrid.AddObstacle(new Position(obstacle.X, obstacle.Y));
                }
                grid = candidateGrid;
            }
        }

        // A target with no known position (no grid_context at all, or
        // this specific candidate wasn't placed) is reported unfiltered
        // — same "no grid, no range check" default every other RPC in
        // this contract already applies, not an exclusion.
        bool InRange(StandardCreature target, int rangeFeet)
        {
            if (grid is null) return true;
            var sourcePos = grid.GetPosition(actor);
            var targetPos = grid.GetPosition(target);
            if (sourcePos is null || targetPos is null) return true;
            return grid.GetDistance(sourcePos.Value, targetPos.Value) <= rangeFeet && grid.HasLineOfSight(sourcePos.Value, targetPos.Value);
        }

        var mainHand = actor.Equipment?.MainHand;
        var offHand = actor.Equipment?.OffHand;

        foreach (var (targetId, targetCreature) in targets)
        {
            if (mainHand is not null)
            {
                if (WeaponAttackRules.IsLegalFor(mainHand, OpenCombatEngine.Core.Enums.AttackKind.Melee, out _) && InRange(targetCreature, mainHand.Range))
                {
                    response.Actions.Add(new AvailableAction
                    {
                        Kind = AvailableActionKind.MeleeAttack,
                        Label = $"Attack {targetCreature.Name} with your {mainHand.Name}",
                        SourceName = mainHand.Name,
                        TargetCharacterId = targetId,
                        ActionEconomyCategory = ActionEconomyCategory.Action,
                    });
                }
                if (WeaponAttackRules.IsLegalFor(mainHand, OpenCombatEngine.Core.Enums.AttackKind.Ranged, out _) && InRange(targetCreature, mainHand.Range))
                {
                    response.Actions.Add(new AvailableAction
                    {
                        Kind = AvailableActionKind.RangedAttack,
                        Label = $"Attack {targetCreature.Name} with your {mainHand.Name}",
                        SourceName = mainHand.Name,
                        TargetCharacterId = targetId,
                        ActionEconomyCategory = ActionEconomyCategory.Action,
                    });
                }
            }

            // Off-hand/secondary-weapon attack (SRD Two-Weapon Fighting):
            // both weapons must be Light. Bonus action, per SRD's core
            // rule (no ability-modifier feature exception modeled — see
            // this session's own scope decision).
            if (mainHand is not null && offHand is not null
                && mainHand.Properties.Contains(WeaponProperty.Light)
                && offHand.Properties.Contains(WeaponProperty.Light)
                && InRange(targetCreature, offHand.Range))
            {
                response.Actions.Add(new AvailableAction
                {
                    Kind = AvailableActionKind.OffhandAttack,
                    Label = $"Attack {targetCreature.Name} with your off-hand {offHand.Name}",
                    SourceName = offHand.Name,
                    TargetCharacterId = targetId,
                    ActionEconomyCategory = ActionEconomyCategory.BonusAction,
                });
            }

            // Grapple: needs a free hand — same approximation
            // GrappleAction itself uses (no Shield/OffHand equipped, and
            // MainHand isn't TwoHanded).
            bool offHandOccupied = offHand is not null || actor.Equipment?.Shield is not null;
            bool mainHandTwoHanded = mainHand?.Properties.Contains(WeaponProperty.TwoHanded) ?? false;
            if (!offHandOccupied && !mainHandTwoHanded && InRange(targetCreature, 5))
            {
                response.Actions.Add(new AvailableAction
                {
                    Kind = AvailableActionKind.Grapple,
                    Label = $"Grapple {targetCreature.Name}",
                    TargetCharacterId = targetId,
                    ActionEconomyCategory = ActionEconomyCategory.Action,
                });
            }

            // Shove: no free-hand requirement, either effect always
            // offered when in reach.
            if (InRange(targetCreature, 5))
            {
                response.Actions.Add(new AvailableAction
                {
                    Kind = AvailableActionKind.ShoveProne,
                    Label = $"Shove {targetCreature.Name} prone",
                    TargetCharacterId = targetId,
                    ActionEconomyCategory = ActionEconomyCategory.Action,
                });
                response.Actions.Add(new AvailableAction
                {
                    Kind = AvailableActionKind.ShovePush,
                    Label = $"Shove {targetCreature.Name} back",
                    TargetCharacterId = targetId,
                    ActionEconomyCategory = ActionEconomyCategory.Action,
                });
            }

            // Single-target prepared/known spells (PreparedSpells already
            // falls back to KnownSpells for a non-prepared caster — no
            // extra branching needed) with an available slot — the
            // cantrip (level 0) exception matches CastSpellAction's own
            // ApplySpellEffects level-0 handling: cantrips need no slot.
            if (actor.Spellcasting is not null)
            {
                foreach (var spell in actor.Spellcasting.PreparedSpells)
                {
                    bool needsTarget = spell.AreaOfEffect is null && !spell.Range.Equals("Self", System.StringComparison.OrdinalIgnoreCase);
                    if (!needsTarget) continue; // self/AOE spells get one targetless entry below, not per-target

                    bool hasSlot = spell.Level == 0 || actor.Spellcasting.HasSlot(spell.Level);
                    if (!hasSlot) continue;

                    var rangeFeet = CastSpellAction.ParseRangeInFeet(spell.Range);
                    if (rangeFeet.HasValue && !InRange(targetCreature, rangeFeet.Value)) continue;

                    response.Actions.Add(new AvailableAction
                    {
                        Kind = AvailableActionKind.CastSpell,
                        Label = $"Cast {spell.Name} at {targetCreature.Name}",
                        SourceName = spell.Name,
                        TargetCharacterId = targetId,
                        ActionEconomyCategory = ActionEconomyCategory.Action,
                    });
                }
            }
        }

        // Self-only and AOE spells — reported once, not per candidate
        // target (there is no single creature to name in the label).
        if (actor.Spellcasting is not null)
        {
            foreach (var spell in actor.Spellcasting.PreparedSpells)
            {
                bool needsTarget = spell.AreaOfEffect is null && !spell.Range.Equals("Self", System.StringComparison.OrdinalIgnoreCase);
                if (needsTarget) continue;

                bool hasSlot = spell.Level == 0 || actor.Spellcasting.HasSlot(spell.Level);
                if (!hasSlot) continue;

                response.Actions.Add(new AvailableAction
                {
                    Kind = AvailableActionKind.CastSpell,
                    Label = $"Cast {spell.Name}",
                    SourceName = spell.Name,
                    ActionEconomyCategory = ActionEconomyCategory.Action,
                });
            }
        }

        return Task.FromResult(response);
    }

    public override Task<CharacterCreationPromptResponse> StartCharacterCreation(
        StartCharacterCreationRequest request, ServerCallContext context)
    {
        var mode = MapMode(request.Mode);
        var result = _characterCreationService.Start(request.SessionId, mode, request.CharacterName);
        return Task.FromResult(MapCreationPrompt(result));
    }

    public override Task<CharacterCreationPromptResponse> AnswerCharacterCreationPrompt(
        AnswerCharacterCreationPromptRequest request, ServerCallContext context)
    {
        var result = _characterCreationService.Answer(request.SessionId, request.Answer);
        return Task.FromResult(MapCreationPrompt(result));
    }

    public override Task<ListClassSpellsResponse> ListClassSpells(
        ListClassSpellsRequest request, ServerCallContext context)
    {
        if (_characterCreationService is not StandardCharacterCreationService standardService)
        {
            // Every real registration in Program.cs is this concrete type;
            // a different ICharacterCreationService implementation simply
            // doesn't support this convenience lookup.
            return Task.FromResult(new ListClassSpellsResponse { Success = false, Error = "ListClassSpells is not supported by the configured character creation service." });
        }

        var (cantrips, leveled) = standardService.ListClassSpells(request.ClassName);
        var response = new ListClassSpellsResponse { Success = true };
        response.Cantrips.AddRange(cantrips);
        response.LeveledSpells.AddRange(leveled);
        return Task.FromResult(response);
    }

    private static OpenCombatEngine.Core.Interfaces.CharacterCreation.CharacterCreationMode MapMode(CharacterCreationMode mode) => mode switch
    {
        CharacterCreationMode.Quick => OpenCombatEngine.Core.Interfaces.CharacterCreation.CharacterCreationMode.Quick,
        CharacterCreationMode.Detailed => OpenCombatEngine.Core.Interfaces.CharacterCreation.CharacterCreationMode.Detailed,
        _ => OpenCombatEngine.Core.Interfaces.CharacterCreation.CharacterCreationMode.Unspecified,
    };

    private CharacterCreationPromptResponse MapCreationPrompt(OpenCombatEngine.Core.Interfaces.CharacterCreation.CharacterCreationPrompt prompt)
    {
        var response = new CharacterCreationPromptResponse
        {
            Success = prompt.Success,
            Error = prompt.Error ?? string.Empty,
            Done = prompt.Done,
            PromptText = prompt.PromptText ?? string.Empty,
        };
        if (prompt.Choices is not null)
            response.Choices.AddRange(prompt.Choices);
        if (prompt.Done && prompt.Character is not null)
            response.Actor = ActorMapping.ToActor(new StandardCreature(prompt.Character, _spellRepository, _itemLibrary));
        if (prompt.AbilityScoreRolls is not null)
        {
            foreach (var set in prompt.AbilityScoreRolls)
            {
                var mappedSet = new AbilityScoreRollSet { Total = set.Total };
                foreach (var die in set.Dice)
                {
                    mappedSet.Dice.Add(new DieRoll { Sides = 6, Result = die.Value, Dropped = die.Dropped });
                }
                response.AbilityScoreRolls.Add(mappedSet);
            }
        }
        return response;
    }

    public override Task StreamEvents(
        StreamEventsRequest request, IServerStreamWriter<EngineEvent> responseStream, ServerCallContext context)
    {
        // Deliberately unimplemented: the engine's per-campaign event feed
        // requires holding a live StandardCombatManager across the stream's
        // lifetime, which conflicts with every other RPC on this service
        // being stateless per-call. Revisit once Master's session model for
        // subscribing to a running combat is designed (docs/design.md §8).
        throw new RpcException(new Status(StatusCode.Unimplemented, "StreamEvents is not yet implemented."));
    }

    private static string? GetString(Struct? s, string key) =>
        s is not null && s.Fields.TryGetValue(key, out var v) && v.KindCase == Value.KindOneofCase.StringValue
            ? v.StringValue
            : null;

    private static double GetNumber(Struct? s, string key) =>
        s is not null && s.Fields.TryGetValue(key, out var v) && v.KindCase == Value.KindOneofCase.NumberValue
            ? v.NumberValue
            : 0;
}
