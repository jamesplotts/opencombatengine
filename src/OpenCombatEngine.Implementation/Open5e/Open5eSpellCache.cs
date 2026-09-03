using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCombatEngine.Implementation.Content.Dtos;

namespace OpenCombatEngine.Implementation.Open5e
{
#pragma warning disable CA1002 // Change List<T> to use Collection<T> — Open5eContentSource.GetAllSpellDtosAsync (this cache's own counterpart) returns the same List<SpellDto> shape; SpellDtos.cs disables the same rule for the same "internal DTO plumbing, not a public collection contract" reasoning.
    /// <summary>
    /// A local on-disk cache of Open5e's spell list, stored as far as
    /// <see cref="SpellDto"/> (not yet the final <c>ISpell</c> — mapping
    /// that last step needs a real <c>IDiceRoller</c>, supplied by
    /// whoever loads this cache). Exists so a repeated sidecar startup
    /// doesn't need to re-fetch all ~1400 SRD spells from the live
    /// Open5e API every single time — a real, observed problem: repeated
    /// local runs against a slow, rate-limited, or briefly unreachable
    /// Open5e can leave the sidecar starting with an empty spell
    /// repository, or taking a long time to give up trying.
    ///
    /// SRD spell content is essentially static, so staleness is checked
    /// by age alone (no network round trip needed just to ask "did
    /// anything change" — a conditional-request/ETag scheme would still
    /// have to reach Open5e at all to answer that, defeating the point).
    /// </summary>
    public static class Open5eSpellCache
    {
        private sealed class CacheFile
        {
            [JsonPropertyName("fetchedAtUtc")]
            public DateTime FetchedAtUtc { get; set; }

            [JsonPropertyName("spells")]
            public List<SpellDto> Spells { get; set; } = new();
        }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
        };

        /// <summary>
        /// Loads cached spells from path if the file exists, is
        /// readable, and is no older than maxAge. Returns null — not an
        /// empty list — for "missing," "corrupt," and "too old" alike,
        /// so a caller has one check ("was anything usable returned")
        /// rather than needing to distinguish those cases itself.
        /// </summary>
        public static List<SpellDto>? TryLoadFresh(string path, TimeSpan maxAge)
        {
            var file = TryReadCacheFile(path);
            if (file == null)
            {
                return null;
            }
            return DateTime.UtcNow - file.FetchedAtUtc <= maxAge ? file.Spells : null;
        }

        /// <summary>
        /// Loads cached spells from path regardless of age — the
        /// "better than nothing" fallback for when a live refresh
        /// attempt has already failed (Open5e unreachable): a stale
        /// cache is still far more useful than an empty spell
        /// repository. Returns null if the file is missing, corrupt, or
        /// otherwise unreadable.
        /// </summary>
        public static List<SpellDto>? TryLoadAny(string path)
        {
            return TryReadCacheFile(path)?.Spells;
        }

        /// <summary>
        /// How old the cache at path is, or null if it doesn't exist or
        /// can't be read — purely informational, for a startup log line
        /// explaining which path was taken.
        /// </summary>
        public static TimeSpan? Age(string path)
        {
            var file = TryReadCacheFile(path);
            return file == null ? null : DateTime.UtcNow - file.FetchedAtUtc;
        }

        private static CacheFile? TryReadCacheFile(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
#pragma warning disable CA1031
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<CacheFile>(json, SerializerOptions);
            }
            catch (Exception)
            {
                // A corrupt/malformed cache file is treated identically
                // to "no cache," not a startup failure — a fresh live
                // fetch (or the "nothing available" degraded path)
                // takes over from here.
                return null;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Writes spells to path as the current cache, overwriting
        /// whatever was there and creating the containing directory if
        /// needed. Best-effort: a failure to write (e.g. a read-only
        /// filesystem) is logged and swallowed, not thrown — losing the
        /// ability to cache shouldn't fail a startup that just
        /// successfully fetched real data live.
        /// </summary>
        public static void Save(string path, IReadOnlyList<SpellDto> spells)
        {
#pragma warning disable CA1031
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var file = new CacheFile { FetchedAtUtc = DateTime.UtcNow, Spells = new List<SpellDto>(spells) };
                File.WriteAllText(path, JsonSerializer.Serialize(file, SerializerOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: failed to write Open5e spell cache to '{path}': {ex.Message}");
            }
#pragma warning restore CA1031
        }
    }
#pragma warning restore CA1002
}
