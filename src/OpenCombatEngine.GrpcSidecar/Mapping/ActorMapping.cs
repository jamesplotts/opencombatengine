// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Layforge.Protocol.SystemEngine.V1;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Creatures;

namespace OpenCombatEngine.GrpcSidecar.Mapping;

/// <summary>
/// Converts between the System Engine gRPC contract's <see cref="Actor"/>
/// message and a live <see cref="StandardCreature"/>, by way of
/// <see cref="CreatureStateJson"/> and <see cref="StructJson"/>.
/// </summary>
/// <remarks>
/// Every RPC that carries an Actor is stateless per-call (docs/design.md
/// §3.1, §10, and the proto's own ApplyEffectRequest/GetCharacterStatusRequest/
/// ToJsonRequest comments): the sidecar holds no creature state of its own
/// between calls, so each request reconstructs a full StandardCreature from
/// the Actor it was sent and each response serializes a full StandardCreature
/// back out — there is nothing here for a bare actor_id to look up.
/// </remarks>
public static class ActorMapping
{
    /// <summary>
    /// The schema_version this engine's Actor.character_data conforms to.
    /// </summary>
    public const string SchemaVersion = "opencombatengine-v1";

    /// <summary>
    /// Converts a live creature to its Actor wire representation.
    /// </summary>
    /// <param name="creature">The creature to convert.</param>
    /// <returns>An Actor carrying creature's full current state.</returns>
    public static Actor ToActor(StandardCreature creature)
    {
        ArgumentNullException.ThrowIfNull(creature);

        var json = CreatureStateJson.Serialize(creature.GetState());
        return new Actor
        {
            ActorId = creature.Id.ToString(),
            CharacterData = StructJson.FromJson(json),
            SchemaVersion = SchemaVersion,
            Level = creature.LevelManager.TotalLevel,
        };
    }

    /// <summary>
    /// Reconstructs a live creature from an Actor's character_data.
    /// </summary>
    /// <param name="actor">The actor to convert.</param>
    /// <param name="spellRepository">
    /// Resolves the spell names in actor's character_data (known/prepared
    /// spells, and any spell currently being concentrated on) back into live
    /// <see cref="ISpell"/> instances. Without one, <see cref="StandardCreature"/>'s
    /// state-restoring constructor cannot rebuild spellcasting state at all —
    /// see that constructor's own remarks — so every caller of this method
    /// must supply a real, populated repository (see
    /// <c>Program.cs</c>'s startup wiring) for spellcasting to survive this
    /// round trip.
    /// </param>
    /// <param name="itemLibrary">
    /// Resolves the item names in actor's character_data (inventory,
    /// equipped slots) back into live <see cref="IItem"/>/<see cref="IWeapon"/>/
    /// <see cref="IArmor"/> instances carrying their real stats. Without one
    /// (the default, <c>null</c>), <see cref="StandardCreature"/>'s
    /// state-restoring constructor falls back to a bare placeholder
    /// <c>StandardItem</c> for every inventory/equipped item — which is not
    /// an <see cref="IWeapon"/>, so equipping it silently fails and
    /// <c>Equipment.MainHand</c> comes back <c>null</c> regardless of what
    /// was actually equipped before serialization. A caller resolving an
    /// Attacker for the <c>Attack</c> RPC must supply a real, populated
    /// library (see <c>Program.cs</c>'s startup wiring) for equipped-weapon
    /// data to survive this round trip at all.
    /// </param>
    /// <returns>
    /// A success Result carrying the reconstructed creature, or a failure
    /// Result carrying a human-readable error — never throws, since callers
    /// (the gRPC service methods) need to translate a malformed Actor into
    /// a response's own error field rather than an unhandled exception
    /// crossing the RPC boundary.
    /// </returns>
    public static Result<StandardCreature> ToCreature(Actor actor, ISpellRepository spellRepository, IItemLibrary? itemLibrary = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(spellRepository);

        if (actor.CharacterData is null)
            return Result<StandardCreature>.Failure("Missing required field 'character_data'.");

        string json;
        try
        {
            json = StructJson.ToJson(actor.CharacterData);
        }
        catch (Google.Protobuf.InvalidJsonException ex)
        {
            return Result<StandardCreature>.Failure(ex.Message);
        }

        var stateResult = CreatureStateJson.Deserialize(json);
        if (stateResult.IsFailure)
            return Result<StandardCreature>.Failure(stateResult.Error);

        try
        {
            return Result<StandardCreature>.Success(new StandardCreature(stateResult.Value, spellRepository, itemLibrary));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<StandardCreature>.Failure(ex.Message);
        }
    }
}
