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

        // The four tests below are the actual regression coverage for the
        // bug live testing found: every Open5e-sourced spell dealt zero
        // damage, because Open5eAdapter.ToStandard never populated
        // SpellDto.Damage/DamageInflict/SavingThrow from the real API's
        // only source of that information (desc's free text) — see
        // Open5eSpellTextParserTests for the parsing logic itself; these
        // confirm it's actually wired into the end-to-end mapping a real
        // spell repository population goes through.
        [Fact]
        public async Task GetAllSpellsAsync_SpellWithDamageInDescription_MappedSpellHasRealDamageRoll()
        {
            var page = new Open5eListResult<Open5eSpell> { Count = 1 };
            page.Results.Add(new Open5eSpell
            {
                Name = "Magic Missile",
                LevelInt = 1,
                School = "evocation",
                Range = "120 feet",
                CastingTime = "1 action",
                Duration = "Instantaneous",
                Desc = "A dart deals 1d4 + 1 force damage to its target. The darts all strike simultaneously.",
            });
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page));

            var spells = await _contentSource.GetAllSpellsAsync();

            var spell = spells.Should().ContainSingle(s => s.Name == "Magic Missile").Subject;
            spell.DamageRolls.Should().ContainSingle();
            spell.DamageRolls[0].Dice.Should().Be("1d4+1");
            spell.DamageRolls[0].Type.Should().Be(OpenCombatEngine.Core.Enums.DamageType.Force);
        }

        [Fact]
        public async Task GetAllSpellsAsync_SaveForHalfSpell_MappedSpellHasSaveAbilityAndHalfDamageEffect()
        {
            var page = new Open5eListResult<Open5eSpell> { Count = 1 };
            page.Results.Add(new Open5eSpell
            {
                Name = "Fireball",
                LevelInt = 3,
                School = "evocation",
                Range = "150 feet",
                CastingTime = "1 action",
                Duration = "Instantaneous",
                Desc = "Each creature in a 20-foot-radius sphere centered on that point must make a dexterity saving throw. A target takes 8d6 fire damage on a failed save, or half as much damage on a successful one.",
            });
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page));

            var spells = await _contentSource.GetAllSpellsAsync();

            var spell = spells.Should().ContainSingle(s => s.Name == "Fireball").Subject;
            spell.SaveAbility.Should().Be(OpenCombatEngine.Core.Enums.Ability.Dexterity);
            spell.SaveEffect.Should().Be(OpenCombatEngine.Core.Enums.SaveEffect.HalfDamage);
            spell.DamageRolls.Should().ContainSingle();
            spell.DamageRolls[0].Dice.Should().Be("8d6");
            spell.DamageRolls[0].Type.Should().Be(OpenCombatEngine.Core.Enums.DamageType.Fire);
        }

        [Fact]
        public async Task GetAllSpellsAsync_NegateOnSaveSpell_MappedSpellHasNegateSaveEffect()
        {
            var page = new Open5eListResult<Open5eSpell> { Count = 1 };
            page.Results.Add(new Open5eSpell
            {
                Name = "Sacred Flame",
                LevelInt = 0,
                School = "evocation",
                Range = "60 feet",
                CastingTime = "1 action",
                Duration = "Instantaneous",
                Desc = "Flame-like radiance descends on a creature that you can see within range. The target must succeed on a dexterity saving throw or take 1d8 radiant damage. The target gains no benefit from cover for this saving throw.",
            });
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page));

            var spells = await _contentSource.GetAllSpellsAsync();

            var spell = spells.Should().ContainSingle(s => s.Name == "Sacred Flame").Subject;
            spell.SaveAbility.Should().Be(OpenCombatEngine.Core.Enums.Ability.Dexterity);
            spell.SaveEffect.Should().Be(OpenCombatEngine.Core.Enums.SaveEffect.Negate);
        }

        [Fact]
        public async Task GetAllSpellsAsync_SpellWithNoDamageInDescription_MappedSpellHasNoDamageRolls()
        {
            var page = new Open5eListResult<Open5eSpell> { Count = 1 };
            page.Results.Add(new Open5eSpell
            {
                Name = "Mage Armor",
                LevelInt = 1,
                School = "abjuration",
                Range = "Touch",
                CastingTime = "1 action",
                Duration = "8 hours",
                Desc = "You touch a willing creature who isn't wearing armor, and a protective magical force surrounds it until the spell ends.",
            });
            _mockClient.GetSpellsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eSpell>?>(page));

            var spells = await _contentSource.GetAllSpellsAsync();

            var spell = spells.Should().ContainSingle(s => s.Name == "Mage Armor").Subject;
            spell.DamageRolls.Should().BeEmpty();
            spell.SaveAbility.Should().BeNull();
        }
    }
}
