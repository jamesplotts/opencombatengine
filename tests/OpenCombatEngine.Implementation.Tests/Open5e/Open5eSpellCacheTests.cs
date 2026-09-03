using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using OpenCombatEngine.Implementation.Content.Dtos;
using OpenCombatEngine.Implementation.Open5e;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Open5e
{
    /// <summary>
    /// Tests for <see cref="Open5eSpellCache"/> — the local on-disk cache
    /// added after a real, observed problem: a repeated sidecar startup
    /// re-fetching ~1400 SRD spells from Open5e every single time got
    /// this development environment rate-limited/blocked by Open5e's own
    /// Cloudflare front, leaving the sidecar unable to start at all.
    /// </summary>
    public class Open5eSpellCacheTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _cachePath;

        public Open5eSpellCacheTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ocegrepcachetests-" + Guid.NewGuid().ToString("N"));
            _cachePath = Path.Combine(_tempDir, "spells.json");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        private static SpellDto MakeSpellDto(string name) => new() { Name = name, Level = 1 };

        // Writes a cache file directly (bypassing Save) with a caller-
        // chosen fetchedAtUtc, for staleness tests that need a specific
        // age Save's own "always now" timestamp can't produce.
        private void WriteRawCacheFile(DateTime fetchedAtUtc, IEnumerable<string> spellNames)
        {
            Directory.CreateDirectory(_tempDir);
            var names = string.Join(",", System.Linq.Enumerable.Select(spellNames, n => $"{{\"name\":\"{n}\",\"level\":1}}"));
            var json = $"{{\"fetchedAtUtc\":\"{fetchedAtUtc:O}\",\"spells\":[{names}]}}";
            File.WriteAllText(_cachePath, json);
        }

        [Fact]
        public void Save_ThenTryLoadFresh_WithinMaxAge_ReturnsSavedSpells()
        {
            var spells = new List<SpellDto> { MakeSpellDto("Magic Missile"), MakeSpellDto("Fireball") };

            Open5eSpellCache.Save(_cachePath, spells);
            var loaded = Open5eSpellCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().NotBeNull();
            loaded!.Should().HaveCount(2);
            loaded.Should().Contain(s => s.Name == "Magic Missile");
            loaded.Should().Contain(s => s.Name == "Fireball");
        }

        [Fact]
        public void TryLoadFresh_CacheOlderThanMaxAge_ReturnsNull()
        {
            WriteRawCacheFile(DateTime.UtcNow.AddDays(-10), new[] { "Magic Missile" });

            var loaded = Open5eSpellCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().BeNull();
        }

        [Fact]
        public void TryLoadFresh_CacheWithinMaxAge_ReturnsSpells()
        {
            WriteRawCacheFile(DateTime.UtcNow.AddDays(-2), new[] { "Magic Missile" });

            var loaded = Open5eSpellCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().NotBeNull();
            loaded!.Should().ContainSingle(s => s.Name == "Magic Missile");
        }

        [Fact]
        public void TryLoadFresh_NoFileExists_ReturnsNull()
        {
            var loaded = Open5eSpellCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().BeNull();
        }

        [Fact]
        public void TryLoadFresh_CorruptFile_ReturnsNullNotThrows()
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(_cachePath, "{not valid json");

            var loaded = Open5eSpellCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().BeNull();
        }

        [Fact]
        public void TryLoadAny_ReturnsCacheRegardlessOfAge()
        {
            WriteRawCacheFile(DateTime.UtcNow.AddDays(-365), new[] { "Ancient Spell" });

            var loaded = Open5eSpellCache.TryLoadAny(_cachePath);

            loaded.Should().NotBeNull();
            loaded!.Should().ContainSingle(s => s.Name == "Ancient Spell");
        }

        [Fact]
        public void TryLoadAny_NoFileExists_ReturnsNull()
        {
            Open5eSpellCache.TryLoadAny(_cachePath).Should().BeNull();
        }

        [Fact]
        public void Age_ExistingFreshFile_ReturnsSmallElapsedTime()
        {
            Open5eSpellCache.Save(_cachePath, new List<SpellDto> { MakeSpellDto("Magic Missile") });

            var age = Open5eSpellCache.Age(_cachePath);

            age.Should().NotBeNull();
            age!.Value.Should().BeLessThan(TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void Age_NoFileExists_ReturnsNull()
        {
            Open5eSpellCache.Age(_cachePath).Should().BeNull();
        }

        [Fact]
        public void Save_ContainingDirectoryDoesNotExist_CreatesItAndWrites()
        {
            Directory.Exists(_tempDir).Should().BeFalse("the directory must not exist yet for this test to prove anything");

            Open5eSpellCache.Save(_cachePath, new List<SpellDto> { MakeSpellDto("Magic Missile") });

            File.Exists(_cachePath).Should().BeTrue();
        }

        [Fact]
        public void Save_ThenLoad_PreservesDamageAndOtherMappedFields()
        {
            // A round-trip regression guard: the cache must survive more
            // than just Name/Level, or a cached-vs-live spell would
            // silently behave differently once mapped through
            // SpellMapper (e.g. losing damage data entirely).
            var spell = new SpellDto
            {
                Name = "Magic Missile",
                Level = 1,
                Damage = new List<List<string>> { new() { "1d4+1" } },
                DamageInflict = new List<string> { "FORCE" },
                InstanceCount = 3,
                InstanceCountPerUpcastLevel = 1,
            };

            Open5eSpellCache.Save(_cachePath, new List<SpellDto> { spell });
            var loaded = Open5eSpellCache.TryLoadFresh(_cachePath, TimeSpan.FromDays(7));

            loaded.Should().NotBeNull();
            var reloaded = loaded!.Should().ContainSingle().Subject;
            reloaded.Damage.Should().NotBeNull();
            reloaded.Damage![0][0].Should().Be("1d4+1");
            reloaded.DamageInflict.Should().Equal("FORCE");
            reloaded.InstanceCount.Should().Be(3);
            reloaded.InstanceCountPerUpcastLevel.Should().Be(1);
        }
    }
}
