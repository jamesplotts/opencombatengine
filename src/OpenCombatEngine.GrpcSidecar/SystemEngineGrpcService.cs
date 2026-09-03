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

    /// <summary>
    /// Constructs the service. <paramref name="spellRepository"/>,
    /// <paramref name="diceRoller"/>, and <paramref name="itemLibrary"/> are
    /// resolved by ASP.NET Core's DI container (gRPC service instances are
    /// DI-constructed) — see <c>Program.cs</c> for where the singleton
    /// spell repository and item library instances are populated from
    /// Open5e at startup and registered.
    /// </summary>
    public SystemEngineGrpcService(ISpellRepository spellRepository, IDiceRoller diceRoller, IItemLibrary itemLibrary)
    {
        _spellRepository = spellRepository ?? throw new System.ArgumentNullException(nameof(spellRepository));
        _diceRoller = diceRoller ?? throw new System.ArgumentNullException(nameof(diceRoller));
        _itemLibrary = itemLibrary ?? throw new System.ArgumentNullException(nameof(itemLibrary));
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
            return Task.FromResult(new AttackResponse { Success = false, Error = "kind must be ATTACK_KIND_MELEE or ATTACK_KIND_RANGED." });

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

        var weapon = attacker.Equipment?.MainHand;
        if (weapon is null)
            return Task.FromResult(new AttackResponse { Success = false, Error = "No weapon equipped — melee_attack/ranged_attack requires a real weapon in the attacker's main hand." });

        // The real gate: a weapon's own SRD properties, not the DM's own
        // judgment, decide whether it can be used this way — see
        // WeaponAttackRules (shared with GetAvailableActions and
        // off-hand-attack construction so they can't drift apart).
        var domainKind = request.Kind == Layforge.Protocol.SystemEngine.V1.AttackKind.Melee ? OpenCombatEngine.Core.Enums.AttackKind.Melee : OpenCombatEngine.Core.Enums.AttackKind.Ranged;
        if (!WeaponAttackRules.IsLegalFor(weapon, domainKind, out var illegalReason))
            return Task.FromResult(new AttackResponse { Success = false, Error = illegalReason });

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

        var action = WeaponAttackRules.BuildAttackAction(attacker, weapon, domainKind, _diceRoller);
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
