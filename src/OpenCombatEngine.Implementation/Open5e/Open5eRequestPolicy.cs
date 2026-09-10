// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;

namespace OpenCombatEngine.Implementation.Open5e
{
    /// <summary>
    /// Retry/timeout settings for <see cref="Open5eClient"/>'s HTTP calls to
    /// api.open5e.com.
    /// </summary>
    /// <remarks>
    /// The timeout here is <em>per attempt</em>, not a budget for the whole
    /// (paginated, multi-request) fetch — the reason the previous single
    /// <see cref="System.Net.Http.HttpClient.Timeout"/> approach failed a
    /// cold-cache sidecar startup whenever Open5e's Cloudflare front was
    /// slow: one 15-second budget across ~14 spell-list pages meant a
    /// couple of sluggish pages exhausted it and the whole fetch threw,
    /// with nothing cached to fall back on. <see cref="Open5eClient"/>
    /// retries the transient failures (timeout, connection error,
    /// <c>429</c>, <c>5xx</c>) with exponential backoff instead.
    /// </remarks>
    public sealed record Open5eRequestPolicy
    {
        /// <summary>
        /// Total attempts per request, including the first. Must be at
        /// least 1. Default 4.
        /// </summary>
        public int MaxAttempts { get; init; } = 4;

        /// <summary>
        /// How long a single attempt may take before it is cancelled and
        /// (if attempts remain) retried. Default 20 seconds.
        /// </summary>
        public TimeSpan PerAttemptTimeout { get; init; } = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Base delay for exponential backoff between attempts
        /// (<c>Base * 2^(attempt-1)</c>, plus jitter, capped at
        /// <see cref="MaxRetryDelay"/>). A <c>429</c> response's
        /// <c>Retry-After</c> header, when present and longer, wins.
        /// Default 2 seconds.
        /// </summary>
        public TimeSpan BaseRetryDelay { get; init; } = TimeSpan.FromSeconds(2);

        /// <summary>Ceiling on any single backoff wait. Default 30 seconds.</summary>
        public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>The default policy.</summary>
        public static Open5eRequestPolicy Default { get; } = new();
    }
}
