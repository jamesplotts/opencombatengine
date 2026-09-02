// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using Layforge.Protocol.SystemEngine.V1;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.GrpcSidecar.Mapping;

/// <summary>
/// Maps an <see cref="IHitPoints"/> component's state to the System Engine
/// gRPC contract's <see cref="CharacterStatus"/> enum, driving Master's
/// turn-order state machine (docs/design.md §3.1, §9.3).
/// </summary>
public static class CharacterStatusMapper
{
    /// <summary>
    /// Maps hitPoints to the character's current mechanical status.
    /// </summary>
    /// <param name="hitPoints">The creature's hit points component.</param>
    /// <returns>The corresponding CharacterStatus.</returns>
    public static CharacterStatus Map(IHitPoints hitPoints)
    {
        ArgumentNullException.ThrowIfNull(hitPoints);

        if (hitPoints.IsDead)
            return CharacterStatus.Dead;

        if (hitPoints.Current > 0)
            return CharacterStatus.Active;

        // At or below 0 HP: stable means stabilized (no longer rolling
        // death saves), distinct from dying (actively rolling them) —
        // see IHitPoints.IsStable / IHitPoints.Downed vs. Died.
        return hitPoints.IsStable ? CharacterStatus.Unconscious : CharacterStatus.Dying;
    }
}
