// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenCombatEngine.Implementation.Open5e.Models;

namespace OpenCombatEngine.Implementation.Open5e
{
    /// <summary>
    /// A thin, resilient HTTP client for api.open5e.com. Every request is
    /// bounded by a <em>per-attempt</em> timeout and retried with
    /// exponential backoff on the failures that are actually transient
    /// (a timed-out attempt, a connection error, an HTTP <c>429</c> or
    /// <c>5xx</c>). A request that still can't succeed throws
    /// <see cref="Open5eUnavailableException"/>; a real <c>404</c> (and any
    /// other non-retriable non-success) comes back as <see langword="null"/>
    /// so a slug lookup can treat it as "not found".
    /// </summary>
    public class Open5eClient
    {
        private readonly HttpClient _httpClient;
        private readonly Open5eRequestPolicy _policy;
        private const string BaseUrl = "https://api.open5e.com/v1/";

        /// <summary>
        /// Creates an <see cref="Open5eClient"/> with the default retry
        /// policy (<see cref="Open5eRequestPolicy.Default"/>).
        /// </summary>
        /// <remarks>
        /// An explicit single-argument overload, not just a default
        /// parameter on the two-argument constructor below, so that
        /// <c>Substitute.For&lt;Open5eClient&gt;(httpClient)</c> in the test
        /// suite finds an exact constructor match (Castle DynamicProxy does
        /// not fill in optional parameters).
        /// </remarks>
        public Open5eClient(HttpClient httpClient)
            : this(httpClient, null)
        {
        }

        /// <summary>
        /// Creates an <see cref="Open5eClient"/>. <paramref name="policy"/>
        /// defaults to <see cref="Open5eRequestPolicy.Default"/>. The
        /// caller's <paramref name="httpClient"/> should leave
        /// <see cref="HttpClient.Timeout"/> at
        /// <see cref="Timeout.InfiniteTimeSpan"/> (or generously high) —
        /// timing out an attempt is this client's job now, per-attempt, not
        /// the whole paginated fetch's.
        /// </summary>
        public Open5eClient(HttpClient httpClient, Open5eRequestPolicy? policy)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _policy = policy ?? Open5eRequestPolicy.Default;
        }

        /// <summary>
        /// Fetches one SRD spell by slug. A <see langword="null"/> return
        /// means "couldn't get it" — an unknown slug, a malformed response,
        /// or (unlike the bulk list fetchers) the service being unreachable
        /// after retries; a single-slug caller can't act on the difference.
        /// </summary>
        public virtual async Task<Open5eSpell?> GetSpellAsync(string slug)
        {
            return await GetItemBySlugAsync<Open5eSpell>("spells", slug).ConfigureAwait(false);
        }

        /// <summary>
        /// Fetches one SRD monster by slug. Same return contract as
        /// <see cref="GetSpellAsync"/>.
        /// </summary>
        public virtual async Task<Open5eMonster?> GetMonsterAsync(string slug)
        {
            return await GetItemBySlugAsync<Open5eMonster>("monsters", slug).ConfigureAwait(false);
        }

        private async Task<T?> GetItemBySlugAsync<T>(string endpoint, string slug)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            try
            {
                var uri = new Uri($"{BaseUrl}{endpoint}/{slug}/");
                var json = await GetStringOrNullAsync(uri).ConfigureAwait(false);
                return json == null ? null : JsonSerializer.Deserialize<T>(json);
            }
            catch (Open5eUnavailableException)
            {
                // A single-slug lookup can't act on "the service is down"
                // any differently than "not found" — see the exception's
                // own remarks. The bulk list fetchers below deliberately do
                // not catch this.
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Fetches one page of the Open5e SRD spell list.
        /// </summary>
        /// <param name="page">1-based page number, matching Open5e's own pagination.</param>
        /// <returns>The page's results, or <see langword="null"/> if the request
        /// returned a non-retriable non-success (e.g. <c>404</c>) or malformed
        /// JSON — callers should treat that the same as "no more results".
        /// Throws <see cref="Open5eUnavailableException"/> if the service was
        /// unreachable after every retry.</returns>
        public virtual async Task<Open5eListResult<Open5eSpell>?> GetSpellsAsync(int page = 1)
        {
            return await GetListAsync<Open5eSpell>("spells", page).ConfigureAwait(false);
        }

        public virtual async Task<Open5eListResult<Open5eWeapon>?> GetWeaponsAsync(int page = 1)
        {
            return await GetListAsync<Open5eWeapon>("weapons", page).ConfigureAwait(false);
        }

        public virtual async Task<Open5eListResult<Open5eArmor>?> GetArmorAsync(int page = 1)
        {
            return await GetListAsync<Open5eArmor>("armor", page).ConfigureAwait(false);
        }

        public virtual async Task<Open5eListResult<Open5eMagicItem>?> GetMagicItemsAsync(int page = 1)
        {
            return await GetListAsync<Open5eMagicItem>("magicitems", page).ConfigureAwait(false);
        }

        private async Task<Open5eListResult<T>?> GetListAsync<T>(string endpoint, int page)
        {
            try
            {
                var uri = new Uri($"{BaseUrl}{endpoint}/?page={page}");
                var json = await GetStringOrNullAsync(uri).ConfigureAwait(false);
                return json == null ? null : JsonSerializer.Deserialize<Open5eListResult<T>>(json);
            }
            catch (JsonException)
            {
                return null;
            }
            // Open5eUnavailableException is intentionally NOT caught here —
            // a half-fetched SRD list must not look like a complete one to
            // the caller populating a repository from it.
        }

        /// <summary>
        /// GETs <paramref name="uri"/> with a per-attempt timeout and
        /// exponential-backoff retry on transient failure. Returns the
        /// response body on success, <see langword="null"/> on a
        /// non-retriable non-success, or throws
        /// <see cref="Open5eUnavailableException"/> once retries are
        /// exhausted.
        /// </summary>
        private async Task<string?> GetStringOrNullAsync(Uri uri)
        {
            Exception? lastTransient = null;

            for (var attempt = 1; attempt <= _policy.MaxAttempts; attempt++)
            {
                TimeSpan? retryAfterHint = null;
                using (var cts = new CancellationTokenSource(_policy.PerAttemptTimeout))
                {
                    try
                    {
                        using var response = await _httpClient.GetAsync(uri, cts.Token).ConfigureAwait(false);

                        if (response.IsSuccessStatusCode)
                        {
                            return await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                        }

                        if (!IsRetriableStatus(response.StatusCode))
                        {
                            // A real 404 or other non-retriable status —
                            // "not found", not "service down".
                            return null;
                        }

                        lastTransient = new HttpRequestException(
                            $"Open5e returned {(int)response.StatusCode} ({response.StatusCode}) for {uri}");
                        retryAfterHint = response.Headers.RetryAfter?.Delta;
                    }
                    catch (OperationCanceledException ex)
                    {
                        // Per-attempt timeout (cts). TaskCanceledException
                        // derives from this. Retriable.
                        lastTransient = ex;
                    }
                    catch (HttpRequestException ex)
                    {
                        lastTransient = ex;
                    }
                }

                if (attempt < _policy.MaxAttempts)
                {
                    await Task.Delay(BackoffFor(attempt, retryAfterHint)).ConfigureAwait(false);
                }
            }

            throw new Open5eUnavailableException(
                $"Open5e ({uri}) did not respond successfully after {_policy.MaxAttempts} attempts.",
                lastTransient ?? new HttpRequestException("unknown transport failure"));
        }

        private static bool IsRetriableStatus(HttpStatusCode status)
        {
            return status == HttpStatusCode.TooManyRequests || (int)status >= 500;
        }

        /// <summary>
        /// The wait before the attempt that follows a failed
        /// <paramref name="failedAttempt"/> (1-based): full jitter over
        /// <c>[0, Base * 2^(failedAttempt-1)]</c>, capped, but never shorter
        /// than a <c>429</c>'s <c>Retry-After</c>.
        /// </summary>
        private TimeSpan BackoffFor(int failedAttempt, TimeSpan? retryAfter)
        {
            var scaled = _policy.BaseRetryDelay * Math.Pow(2, failedAttempt - 1);
            var capped = scaled > _policy.MaxRetryDelay ? _policy.MaxRetryDelay : scaled;

#pragma warning disable CA5394 // jitter for retry backoff — not security-sensitive
            var jittered = TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * capped.TotalMilliseconds);
#pragma warning restore CA5394
            if (retryAfter.HasValue && retryAfter.Value > jittered)
            {
                return retryAfter.Value > _policy.MaxRetryDelay ? _policy.MaxRetryDelay : retryAfter.Value;
            }
            return jittered;
        }
    }
}
