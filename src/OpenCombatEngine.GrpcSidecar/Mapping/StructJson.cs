// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace OpenCombatEngine.GrpcSidecar.Mapping;

/// <summary>
/// Converts between <see cref="Struct"/> — the wire representation of
/// Actor.character_data on the System Engine gRPC contract — and plain
/// JSON strings, so the rest of the sidecar can work with ordinary JSON
/// (matching how <c>OpenCombatEngine.Implementation.Serialization.CombatSerializer</c>
/// already serializes creature state) instead of protobuf's dynamic
/// <see cref="Struct"/>/<see cref="Value"/> types directly.
/// </summary>
/// <remarks>
/// Google.Protobuf's <see cref="JsonFormatter"/>/<see cref="JsonParser"/>
/// implement protobuf's own well-known-types JSON mapping, under which
/// <see cref="Struct"/> serializes as an ordinary JSON object rather than
/// a protobuf-specific envelope — confirmed by
/// <c>StructJsonTests.ToJson_SimpleStruct_ProducesPlainJsonObject</c>
/// rather than assumed from documentation alone.
/// </remarks>
public static class StructJson
{
    /// <summary>
    /// Converts value to a plain JSON string.
    /// </summary>
    /// <param name="value">The Struct to convert.</param>
    /// <returns>The equivalent plain JSON object, as a string.</returns>
    public static string ToJson(Struct value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonFormatter.Default.Format(value);
    }

    /// <summary>
    /// Parses a plain JSON object string into a <see cref="Struct"/>.
    /// </summary>
    /// <param name="json">A JSON object, e.g. <c>{"name":"Kestrel"}</c>.</param>
    /// <returns>The equivalent Struct.</returns>
    /// <exception cref="InvalidJsonException">
    /// json is not valid JSON, or is not a JSON object.
    /// </exception>
    public static Struct FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonParser.Default.Parse<Struct>(json);
    }
}
