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
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.GrpcSidecar.Mapping;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Spatial;

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

    /// <summary>
    /// Constructs the service. <paramref name="spellRepository"/> and
    /// <paramref name="diceRoller"/> are resolved by ASP.NET Core's DI
    /// container (gRPC service instances are DI-constructed) — see
    /// <c>Program.cs</c> for where the singleton spell repository instance
    /// is populated from Open5e at startup and registered.
    /// </summary>
    public SystemEngineGrpcService(ISpellRepository spellRepository, IDiceRoller diceRoller)
    {
        _spellRepository = spellRepository ?? throw new System.ArgumentNullException(nameof(spellRepository));
        _diceRoller = diceRoller ?? throw new System.ArgumentNullException(nameof(diceRoller));
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
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository);
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

        response.Actor = ActorMapping.ToActor(new StandardCreature(stateResult.Value, _spellRepository));
        return Task.FromResult(response);
    }

    public override Task<GetCharacterStatusResponse> GetCharacterStatus(
        GetCharacterStatusRequest request, ServerCallContext context)
    {
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository);
        if (creatureResult.IsFailure)
            throw new RpcException(new Status(StatusCode.InvalidArgument, creatureResult.Error));

        return Task.FromResult(new GetCharacterStatusResponse
        {
            Status = CharacterStatusMapper.Map(creatureResult.Value.HitPoints),
        });
    }

    public override Task<StartTurnResponse> StartTurn(StartTurnRequest request, ServerCallContext context)
    {
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository);
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
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository);
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
        var creatureResult = ActorMapping.ToCreature(request.Actor, _spellRepository);
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
        var casterResult = ActorMapping.ToCreature(request.Caster, _spellRepository);
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
            var targetResult = ActorMapping.ToCreature(targetActor, _spellRepository);
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
