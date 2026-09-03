// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.Open5e;
using OpenCombatEngine.Implementation.Open5e.Models;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Open5e
{
    /// <summary>
    /// Tests for <see cref="Open5eClient.GetSpellsAsync"/> and
    /// <see cref="Open5eContentSource.GetAllSpellsAsync"/> — the bulk spell
    /// list added to populate a real <see cref="OpenCombatEngine.Core.Interfaces.Spells.ISpellRepository"/>
    /// at gRPC sidecar startup (see OpenCombatEngine.GrpcSidecar's Program.cs).
    /// Same mocking pattern LootGenerationTests already uses for the
    /// existing weapons/armor/magic-items list fetchers.
    /// </summary>
    public class Open5eSpellListTests
    {
        private readonly Open5eClient _mockClient;
        private readonly IDiceRoller _mockDice;
        private readonly Open5eContentSource _contentSource;

        public Open5eSpellListTests()
        {
            var httpClient = new HttpClient();
            _mockClient = Substitute.For<Open5eClient>(httpClient);
            _mockDice = Substitute.For<IDiceRoller>();
            _contentSource = new Open5eContentSource(_mockClient, _mockDice);
        }

        [Fact]
        public async Task GetAllSpellsAsync_SinglePage_ReturnsMappedSpells()
        {
            var page = new Open5eListResult<Open5eSpell> { Count = 1 };
            page.Results.Add(new Open5eSpell { Name = "Magic Missile", LevelInt = 1, School = "evocation", Range = "120 feet", CastingTime = "1 action", Duration = "Instantaneous" });
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page));

            var spells = await _contentSource.GetAllSpellsAsync();

            spells.Should().ContainSingle(s => s.Name == "Magic Missile" && s.Level == 1);
        }

        [Fact]
        public async Task GetAllSpellsAsync_MultiplePages_FollowsNextUntilEmpty()
        {
            var page1 = new Open5eListResult<Open5eSpell> { Count = 2, Next = "https://api.open5e.com/v1/spells/?page=2" };
            page1.Results.Add(new Open5eSpell { Name = "Fireball", LevelInt = 3, School = "evocation", Range = "150 feet", CastingTime = "1 action", Duration = "Instantaneous" });
            var page2 = new Open5eListResult<Open5eSpell> { Count = 2 };
            page2.Results.Add(new Open5eSpell { Name = "Mage Armor", LevelInt = 1, School = "abjuration", Range = "Touch", CastingTime = "1 action", Duration = "8 hours" });
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page1));
            _mockClient.GetSpellsAsync(2).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page2));

            var spells = await _contentSource.GetAllSpellsAsync();

            spells.Should().Contain(s => s.Name == "Fireball");
            spells.Should().Contain(s => s.Name == "Mage Armor");
        }

        [Fact]
        public async Task GetAllSpellsAsync_FirstPageFails_ReturnsEmptyNotThrows()
        {
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(null));

            var spells = await _contentSource.GetAllSpellsAsync();

            spells.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllSpellsAsync_MalformedEntryAmongValidOnes_SkipsItKeepsRest()
        {
            var page = new Open5eListResult<Open5eSpell> { Count = 2 };
            // Open5eAdapter.ToStandard(Open5eSpell) throws
            // ArgumentNullException on a null source — not reachable via a
            // real Open5e response, but exercises this method's own
            // per-entry try/catch without needing to fabricate a more
            // elaborate mapping failure.
            page.Results.Add(null!);
            page.Results.Add(new Open5eSpell { Name = "Light", LevelInt = 0, School = "evocation", Range = "Touch", CastingTime = "1 action", Duration = "1 hour" });
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page));

            var spells = await _contentSource.GetAllSpellsAsync();

            spells.Should().ContainSingle(s => s.Name == "Light");
        }
    }
}
