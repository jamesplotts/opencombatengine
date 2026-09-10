// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;

namespace OpenCombatEngine.Implementation.Open5e
{
    /// <summary>
    /// Thrown by <see cref="Open5eClient"/> when a request to api.open5e.com
    /// still fails after exhausting its retry policy — a timeout on every
    /// attempt, repeated network failure, or repeated <c>429</c>/<c>5xx</c>
    /// responses.
    /// </summary>
    /// <remarks>
    /// Single-item lookups (<see cref="Open5eClient.GetSpellAsync"/>,
    /// <see cref="Open5eClient.GetMonsterAsync"/>) swallow this and return
    /// <see langword="null"/> — a caller resolving one slug can't tell "not
    /// found" from "service down" and shouldn't have to. The bulk list
    /// fetchers (<see cref="Open5eContentSource.GetAllSpellDtosAsync"/> and
    /// the item equivalents) let it propagate: a repository population that
    /// silently stopped halfway would cache a truncated SRD list as if it
    /// were complete. The gRPC sidecar's startup catches it and falls back
    /// to a local cache (see <c>OpenCombatEngine.GrpcSidecar</c>'s
    /// <c>Program.cs</c>).
    /// </remarks>
    public class Open5eUnavailableException : Exception
    {
        /// <summary>Creates an <see cref="Open5eUnavailableException"/>.</summary>
        public Open5eUnavailableException()
        {
        }

        /// <summary>Creates an <see cref="Open5eUnavailableException"/> with a message.</summary>
        public Open5eUnavailableException(string message)
            : base(message)
        {
        }

        /// <summary>Creates an <see cref="Open5eUnavailableException"/> wrapping an inner exception.</summary>
        public Open5eUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
