// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using OpenCombatEngine.Implementation.Open5e;
using OpenCombatEngine.Implementation.Open5e.Models;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Open5e
{
    /// <summary>
    /// Tests for <see cref="Open5eItemCache"/> — mirrors
    /// <see cref="Open5eSpellCacheTests"/>, added after a real, live-
    /// observed problem: a sidecar startup fetching the full, paginated
    /// weapons+armor+magic-items catalog hit the 15-second HttpClient
    /// timeout even with Open5e itself reachable, leaving the item
    /// library empty for that run.
    /// </summary>
    public class Open5eItemCacheTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _cachePath;

        public Open5eItemCacheTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ocegrpcitemcachetests-" + Guid.NewGuid().ToString("N"));
            _cachePath = Path.Combine(_tempDir, "items.json");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private static Open5eWeapon MakeWeapon(string name) => new() { Name = name, Slug = name.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal), DamageDice = "1d8", DamageType = "slashing" };
        private static Open5eArmor MakeArmor(string name) => new() { Name = name, Slug = name.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal), BaseAc = 16 };
        private static Open5eMagicItem MakeMagicItem(string name) => new() { Name = name, Slug = name.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal), Rarity = "Common" };

        // Writes a cache file directly (bypassing Save) with a caller-
        // chosen fetchedAtUtc, for staleness tests that need a specific
        // age Save's own "always now" timestamp can't produce.
        private void WriteRawCacheFile(DateTime fetchedAtUtc)
        {
            Directory.CreateDirectory(_tempDir);
            var json = $"{{\"fetchedAtUtc\":\"{fetchedAtUtc:O}\",\"weapons\":[{{\"name\":\"Longsword\",\"slug\":\"longsword\"}}],\"armor\":[],\"magicItems\":[]}}";
            File.WriteAllText(_cachePath, json);
        }

        [Fact]
        public void Save_ThenTryLoadFresh_WithinMaxAge_ReturnsSavedItems()
        {
            var weapons = new List<Open5eWeapon> { MakeWeapon("Longsword") };
            var armor = new List<Open5eArmor> { MakeArmor("Chain Mail") };
            var magicItems = new List<Open5eMagicItem> { MakeMagicItem("Potion of Healing") };

            Open5eItemCache.Save(_cachePath, weapons, armor, magicItems);
            var loaded = Open5eItemCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().NotBeNull();
            loaded!.Weapons.Should().ContainSingle(w => w.Name == "Longsword");
            loaded.Armor.Should().ContainSingle(a => a.Name == "Chain Mail");
            loaded.MagicItems.Should().ContainSingle(m => m.Name == "Potion of Healing");
        }

        [Fact]
        public void TryLoadFresh_CacheOlderThanMaxAge_ReturnsNull()
        {
            WriteRawCacheFile(DateTime.UtcNow.AddDays(-10));

            var loaded = Open5eItemCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().BeNull();
        }

        [Fact]
        public void TryLoadFresh_CacheWithinMaxAge_ReturnsItems()
        {
            WriteRawCacheFile(DateTime.UtcNow.AddDays(-2));

            var loaded = Open5eItemCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().NotBeNull();
            loaded!.Weapons.Should().ContainSingle(w => w.Name == "Longsword");
        }

        [Fact]
        public void TryLoadFresh_NoFileExists_ReturnsNull()
        {
            var loaded = Open5eItemCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().BeNull();
        }

        [Fact]
        public void TryLoadFresh_CorruptFile_ReturnsNullNotThrows()
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(_cachePath, "{not valid json");

            var loaded = Open5eItemCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().BeNull();
        }

        [Fact]
        public void TryLoadAny_ReturnsCacheRegardlessOfAge()
        {
            WriteRawCacheFile(DateTime.UtcNow.AddDays(-365));

            var loaded = Open5eItemCache.TryLoadAny(_cachePath);

            loaded.Should().NotBeNull();
            loaded!.Weapons.Should().ContainSingle(w => w.Name == "Longsword");
        }

        [Fact]
        public void TryLoadAny_NoFileExists_ReturnsNull()
        {
            Open5eItemCache.TryLoadAny(_cachePath).Should().BeNull();
        }

        [Fact]
        public void Age_ExistingFreshFile_ReturnsSmallElapsedTime()
        {
            Open5eItemCache.Save(_cachePath, new List<Open5eWeapon> { MakeWeapon("Longsword") }, new List<Open5eArmor>(), new List<Open5eMagicItem>());

            var age = Open5eItemCache.Age(_cachePath);

            age.Should().NotBeNull();
            age!.Value.Should().BeLessThan(TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void Age_NoFileExists_ReturnsNull()
        {
            Open5eItemCache.Age(_cachePath).Should().BeNull();
        }

        [Fact]
        public void Save_ContainingDirectoryDoesNotExist_CreatesItAndWrites()
        {
            Directory.Exists(_tempDir).Should().BeFalse("the directory must not exist yet for this test to prove anything");

            Open5eItemCache.Save(_cachePath, new List<Open5eWeapon> { MakeWeapon("Longsword") }, new List<Open5eArmor>(), new List<Open5eMagicItem>());

            File.Exists(_cachePath).Should().BeTrue();
        }

        [Fact]
        public void Save_ThenLoad_PreservesMappedFieldsAcrossAllThreeItemTypes()
        {
            // A round-trip regression guard: the cache must survive more
            // than just Name/Slug, or a cached-vs-live item would
            // silently behave differently once mapped through
            // Open5eItemMapper (e.g. losing damage dice or AC entirely).
            var weapon = new Open5eWeapon { Name = "Longsword", Slug = "longsword", DamageDice = "1d8", DamageType = "slashing", Cost = "15 gp", Weight = "3 lb." };
            weapon.Properties.Add("versatile (1d10)");
            var armor = new Open5eArmor { Name = "Chain Mail", Slug = "chain-mail", Category = "Heavy Armor", BaseAc = 16, Cost = "75 gp", Weight = "55 lb." };
            var magicItem = new Open5eMagicItem { Name = "Potion of Healing", Slug = "potion-of-healing", Type = "Potion", Rarity = "Common", RequiresAttunement = "" };

            Open5eItemCache.Save(_cachePath, new List<Open5eWeapon> { weapon }, new List<Open5eArmor> { armor }, new List<Open5eMagicItem> { magicItem });
            var loaded = Open5eItemCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().NotBeNull();
            var reloadedWeapon = loaded!.Weapons.Should().ContainSingle().Subject;
            reloadedWeapon.DamageDice.Should().Be("1d8");
            reloadedWeapon.Properties.Should().Contain("versatile (1d10)");
            var reloadedArmor = loaded.Armor.Should().ContainSingle().Subject;
            reloadedArmor.BaseAc.Should().Be(16);
            var reloadedMagicItem = loaded.MagicItems.Should().ContainSingle().Subject;
            reloadedMagicItem.Rarity.Should().Be("Common");
        }
    }
}
