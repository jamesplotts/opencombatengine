// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenCombatEngine.Implementation.Open5e.Models;

namespace OpenCombatEngine.Implementation.Open5e
{
#pragma warning disable CA1002 // Change List<T> to use Collection<T> — mirrors Open5eSpellCache's own suppression for the same "internal cache plumbing" reasoning; these DTOs are never a public collection contract in their own right.
    /// <summary>
    /// The three DTO lists a fresh or stale cache load returns together,
    /// since they're always used as a set (see
    /// <see cref="OpenCombatEngine.Implementation.Items.StandardItemLibrary.InitializeFromDtos"/>).
    /// A top-level type (not nested in <see cref="Open5eItemCache"/>) so
    /// it isn't itself flagged as a publicly-visible nested type.
    /// </summary>
    public sealed record Open5eItemDtos(List<Open5eWeapon> Weapons, List<Open5eArmor> Armor, List<Open5eMagicItem> MagicItems);
#pragma warning restore CA1002

    /// <summary>
    /// A local on-disk cache of Open5e's weapon/armor/magic-item lists,
    /// stored as their raw <see cref="Open5eWeapon"/>/<see cref="Open5eArmor"/>/
    /// <see cref="Open5eMagicItem"/> DTOs (not yet the final <c>IItem</c>
    /// instances — mapping that last step needs
    /// <see cref="OpenCombatEngine.Implementation.Content.Mappers.Open5eItemMapper"/>,
    /// applied by whoever loads this cache, e.g.
    /// <see cref="OpenCombatEngine.Implementation.Items.StandardItemLibrary.InitializeFromDtos"/>).
    /// Mirrors <see cref="Open5eSpellCache"/> exactly, for the same real,
    /// observed reason: a repeated local sidecar startup shouldn't need to
    /// re-fetch the whole item catalog from a live, sometimes slow or
    /// rate-limited Open5e API every single time. Unlike the spell cache
    /// (a single list), this holds three lists in one file, since
    /// <c>StandardItemLibrary</c> is populated from all three endpoints
    /// together at startup.
    ///
    /// SRD item content is essentially static, so staleness is checked by
    /// age alone — same reasoning as <see cref="Open5eSpellCache"/>.
    /// </summary>
    public static class Open5eItemCache
    {
#pragma warning disable CA1002 // Change List<T> to use Collection<T> — mirrors Open5eSpellCache.CacheFile's own suppression for the same "internal cache-file plumbing" reasoning.
        private sealed class CacheFile
        {
            [JsonPropertyName("fetchedAtUtc")]
            public DateTime FetchedAtUtc { get; set; }

            [JsonPropertyName("weapons")]
            public List<Open5eWeapon> Weapons { get; set; } = new();

            [JsonPropertyName("armor")]
            public List<Open5eArmor> Armor { get; set; } = new();

            [JsonPropertyName("magicItems")]
            public List<Open5eMagicItem> MagicItems { get; set; } = new();
        }
#pragma warning restore CA1002

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
        };

        /// <summary>
        /// Loads cached item DTOs from path if the file exists, is
        /// readable, and is no older than maxAge. Returns null — not
        /// empty lists — for "missing," "corrupt," and "too old" alike,
        /// mirroring <see cref="Open5eSpellCache.TryLoadFresh"/>.
        /// </summary>
        public static Open5eItemDtos? TryLoadFresh(string path, TimeSpan maxAge)
        {
            var file = TryReadCacheFile(path);
            if (file == null)
            {
                return null;
            }
            return DateTime.UtcNow - file.FetchedAtUtc <= maxAge
                ? new Open5eItemDtos(file.Weapons, file.Armor, file.MagicItems)
                : null;
        }

        /// <summary>
        /// Loads cached item DTOs from path regardless of age — the
        /// "better than nothing" fallback for when a live refresh attempt
        /// has already failed. Returns null if the file is missing,
        /// corrupt, or otherwise unreadable.
        /// </summary>
        public static Open5eItemDtos? TryLoadAny(string path)
        {
            var file = TryReadCacheFile(path);
            return file == null ? null : new Open5eItemDtos(file.Weapons, file.Armor, file.MagicItems);
        }

        /// <summary>
        /// How old the cache at path is, or null if it doesn't exist or
        /// can't be read — purely informational, for a startup log line.
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
                // A corrupt/malformed cache file is treated identically to
                // "no cache," not a startup failure.
                return null;
            }
#pragma warning restore CA1031
        }

        /// <summary>
        /// Writes the three DTO lists to path as the current cache,
        /// overwriting whatever was there and creating the containing
        /// directory if needed. Best-effort: a failure to write is logged
        /// and swallowed, not thrown — mirrors
        /// <see cref="Open5eSpellCache.Save"/>.
        /// </summary>
        public static void Save(string path, IReadOnlyList<Open5eWeapon> weapons, IReadOnlyList<Open5eArmor> armor, IReadOnlyList<Open5eMagicItem> magicItems)
        {
#pragma warning disable CA1031
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var file = new CacheFile
                {
                    FetchedAtUtc = DateTime.UtcNow,
                    Weapons = new List<Open5eWeapon>(weapons),
                    Armor = new List<Open5eArmor>(armor),
                    MagicItems = new List<Open5eMagicItem>(magicItems),
                };
                File.WriteAllText(path, JsonSerializer.Serialize(file, SerializerOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: failed to write Open5e item cache to '{path}': {ex.Message}");
            }
#pragma warning restore CA1031
        }
    }
}
