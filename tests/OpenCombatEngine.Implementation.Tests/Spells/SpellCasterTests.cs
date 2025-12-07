using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Spells;
using OpenCombatEngine.Implementation.Tests.Features; 
// Using SpellCastingTests.FakeSpell? Or create new FakeSpell here? 
// Original SpellCasterTests had "new Spell(...)".
// Spell class used in test likely doesn't exist or was mocked differently?
// Line 94 in original was `new Spell(...)`.
// I don't see `Spell` class in implementation. Maybe it was a test helper in that file or project?
// I'll create a FakeSpell here to be safe.

using Xunit;
using System.Collections.Generic;
using OpenCombatEngine.Core.Models.Spells;
using OpenCombatEngine.Core.Interfaces.Spatial;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class SpellCasterTests
    {
        private class FakeSpell : ISpell
        {
            public string Name { get; }
            public int Level { get; }
            public SpellSchool School { get; } = SpellSchool.Evocation;
            public string CastingTime { get; } = "1 Action";
            public string Range { get; } = "60 ft";
            public string Components { get; } = "V, S";
            public string Duration { get; } = "Instantaneous";
            public string Description { get; } = "A fake spell.";
            public bool RequiresAttackRoll { get; } = false;
            public bool RequiresConcentration { get; } = false;
            public bool Ritual { get; } = false;

            public IShape? AreaOfEffect { get; } = null;
            public Ability? SaveAbility { get; } = null;
            public SaveEffect SaveEffect { get; } = SaveEffect.None;
            public IReadOnlyList<DamageFormula> DamageRolls { get; } = new List<DamageFormula>();
            public string? HealingDice { get; } = null;

            public FakeSpell(string name, int level)
            {
                Name = name;
                Level = level;
            }
            
            public Result<SpellResolution> Cast(ICreature caster, object? target = null)
            {
                return Result<SpellResolution>.Success(new SpellResolution(true, $"Casted {Name}"));
            }
        }

        private readonly ICreature _creature;
        private readonly IAbilityScores _abilityScores;

        public SpellCasterTests()
        {
            _abilityScores = Substitute.For<IAbilityScores>();
            _abilityScores.GetModifier(Arg.Any<Ability>()).Returns(3); // +3 mod
            
            _creature = Substitute.For<ICreature>();
            _creature.AbilityScores.Returns(_abilityScores);
            _creature.ProficiencyBonus.Returns(2);
        }

        private StandardSpellCaster CreateCaster(bool isPrepared = true)
        {
            return new StandardSpellCaster(
                Ability.Intelligence, 
                a => _creature.AbilityScores.GetModifier(a), 
                () => _creature.ProficiencyBonus,
                isPreparedCaster: isPrepared
            );
        }

        [Fact]
        public void Should_Track_Slots()
        {
            var caster = CreateCaster();
            caster.SetSlots(1, 2);

            caster.GetMaxSlots(1).Should().Be(2);
            caster.GetSlots(1).Should().Be(2);
            caster.HasSlot(1).Should().BeTrue();
        }

        [Fact]
        public void ConsumeSlot_Should_Reduce_Slots()
        {
            var caster = CreateCaster();
            caster.SetSlots(1, 2);

            var result = caster.ConsumeSlot(1);

            result.IsSuccess.Should().BeTrue();
            caster.GetSlots(1).Should().Be(1);
        }

        [Fact]
        public void ConsumeSlot_Should_Fail_If_No_Slots()
        {
            var caster = CreateCaster();
            caster.SetSlots(1, 0);

            var result = caster.ConsumeSlot(1);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("No");
        }

        [Fact]
        public void Cantrips_Should_Not_Consume_Slots()
        {
            var caster = CreateCaster();
            var result = caster.ConsumeSlot(0);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void RestoreAllSlots_Should_Reset_To_Max()
        {
            var caster = CreateCaster();
            caster.SetSlots(1, 2);
            caster.ConsumeSlot(1);
            caster.GetSlots(1).Should().Be(1);

            caster.RestoreAllSlots();

            caster.GetSlots(1).Should().Be(2);
        }

        [Fact]
        public void LearnSpell_Should_Add_To_Known()
        {
            var caster = CreateCaster();
            var spell = new FakeSpell("Test", 1);

            caster.LearnSpell(spell);

            caster.KnownSpells.Should().Contain(s => s.Name == "Test");
        }

        [Fact]
        public void Should_Calculate_DC_And_Attack()
        {
            var caster = CreateCaster();
            // Prof 2 + Mod 3 = Attack 5
            // 8 + 5 = DC 13
            caster.SpellAttackBonus.Should().Be(5);
            caster.SpellSaveDC.Should().Be(13);
        }

        [Fact]
        public void PrepareSpell_Should_Add_To_Prepared()
        {
            var caster = CreateCaster(isPrepared: true);
            var spell = new FakeSpell("Test", 1);
            caster.LearnSpell(spell);

            var result = caster.PrepareSpell(spell);

            result.IsSuccess.Should().BeTrue();
            caster.PreparedSpells.Should().Contain(s => s.Name == "Test");
        }

        [Fact]
        public void PreparedSpells_Should_Return_Known_If_Not_PreparedCaster()
        {
            var caster = CreateCaster(isPrepared: false);
            var spell = new FakeSpell("Test", 1);
            caster.LearnSpell(spell);

            // PreparedSpells should return KnownSpells if not prepared caster logic enabled (handled by ternary now)
            caster.PreparedSpells.Should().Contain(s => s.Name == "Test");
            caster.PreparedSpells.Count.Should().Be(1);
            
            // PrepareSpell should detect invalid usage?
            var prepResult = caster.PrepareSpell(spell);
            prepResult.IsSuccess.Should().BeFalse(); // "Caster does not prepare spells."
        }

        [Fact]
        public void PreparedSpells_Should_Return_Only_Prepared_If_PreparedCaster()
        {
            var caster = CreateCaster(isPrepared: true);
            var spell1 = new FakeSpell("Test1", 1);
            var spell2 = new FakeSpell("Test2", 1);
            caster.LearnSpell(spell1);
            caster.LearnSpell(spell2);

            caster.PrepareSpell(spell1);

            caster.PreparedSpells.Should().Contain(s => s.Name == "Test1");
            caster.PreparedSpells.Should().NotContain(s => s.Name == "Test2");
        }
    }
}
