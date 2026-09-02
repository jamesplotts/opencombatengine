// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.GrpcSidecar.Mapping;

/// <summary>
/// Converts between <see cref="CreatureState"/> and JSON strings — the
/// engine's canonical wire representation for Actor.character_data
/// (System Engine gRPC contract's ToJson/FromJson RPCs).
/// </summary>
/// <remarks>
/// Uses camelCase property names, unlike
/// <c>OpenCombatEngine.Implementation.Serialization.CombatSerializer</c>
/// (which serializes with default PascalCase for its own internal
/// save/load format). This deviation is deliberate: this JSON is an
/// external wire contract consumed by Layforge's schema-driven web
/// client (docs/design.md §4), where camelCase is the prevailing
/// convention, whereas CombatSerializer's output never leaves this
/// process.
/// </remarks>
public static class CreatureStateJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Serializes state to its canonical JSON string form.
    /// </summary>
    /// <param name="state">The creature state to serialize.</param>
    /// <returns>The equivalent JSON string.</returns>
    public static string Serialize(CreatureState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(state, Options);
    }

    /// <summary>
    /// Parses a JSON string into a <see cref="CreatureState"/>.
    /// </summary>
    /// <param name="json">A JSON object produced by <see cref="Serialize"/>.</param>
    /// <returns>
    /// A success Result carrying the parsed state, or a failure Result
    /// carrying a human-readable error — never throws for malformed or
    /// ill-shaped input, since callers (the gRPC service methods) need to
    /// translate parse failures into response error fields rather than
    /// unhandled exceptions crossing the RPC boundary.
    /// </returns>
    public static Result<CreatureState> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result<CreatureState>.Failure("json cannot be empty.");

        try
        {
            var state = JsonSerializer.Deserialize<CreatureState>(json, Options);
            if (state is null)
                return Result<CreatureState>.Failure("Deserialized to null.");

            // System.Text.Json's constructor-parameter binding does not
            // throw when a required record parameter is absent from the
            // JSON — it silently passes null instead (confirmed via
            // CreatureStateJsonTests.Deserialize_ValidJsonButWrongShape_ReturnsFailure,
            // not assumed). Check the fields CreatureState's own
            // constructor has no default for explicitly, so malformed
            // input surfaces as a Result failure rather than a
            // CreatureState with silently-null required data.
            if (state.Name is null)
                return Result<CreatureState>.Failure("Missing required field 'name'.");
            if (state.Team is null)
                return Result<CreatureState>.Failure("Missing required field 'team'.");
            if (state.AbilityScores is null)
                return Result<CreatureState>.Failure("Missing required field 'abilityScores'.");
            if (state.HitPoints is null)
                return Result<CreatureState>.Failure("Missing required field 'hitPoints'.");

            return Result<CreatureState>.Success(state);
        }
        catch (JsonException ex)
        {
            return Result<CreatureState>.Failure(ex.Message);
        }
    }
}
