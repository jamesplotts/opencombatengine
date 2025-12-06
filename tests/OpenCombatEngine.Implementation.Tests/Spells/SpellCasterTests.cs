using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class SpellCasterTests
    {
        private readonly ICreature _creature;
        private readonly IAbilityScores _abilityScores;
        private readonly OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller _diceRoller = Substitute.For<OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller>();

        public SpellCasterTests()
        {
            _abilityScores = Substitute.For<IAbilityScores>();
            _abilityScores.GetModifier(Arg.Any<Ability>()).Returns(3); // +3 mod
            
            _creature = Substitute.For<ICreature>();
            _creature.AbilityScores.Returns(_abilityScores);
            _creature.ProficiencyBonus.Returns(2);
        }

        private StandardSpellCaster CreateCaster()
        {
            return new StandardSpellCaster(_creature, Ability.Intelligence);
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
            result.Error.Should().Contain("No spell slots");
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
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", _diceRoller);

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
            var caster = new StandardSpellCaster(_creature, Ability.Intelligence, isPreparedCaster: true);
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", _diceRoller);
            caster.LearnSpell(spell);

            var result = caster.PrepareSpell(spell);

            result.IsSuccess.Should().BeTrue();
            caster.PreparedSpells.Should().Contain(s => s.Name == "Test");
        }

        [Fact]
        public void PreparedSpells_Should_Return_Known_If_Not_PreparedCaster()
        {
            var caster = new StandardSpellCaster(_creature, Ability.Intelligence, isPreparedCaster: false);
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", _diceRoller);
            caster.LearnSpell(spell);

            // Even if we prepare it (which we can technically do), PreparedSpells should return KnownSpells
            // Wait, my implementation says: _isPreparedCaster ? _preparedSpells : _knownSpells
            // So if false, it returns KnownSpells.
            caster.PreparedSpells.Should().Contain(s => s.Name == "Test");
            caster.PreparedSpells.Count.Should().Be(1);
        }

        [Fact]
        public void PreparedSpells_Should_Return_Only_Prepared_If_PreparedCaster()
        {
            var caster = new StandardSpellCaster(_creature, Ability.Intelligence, isPreparedCaster: true);
            var spell1 = new Spell("Test1", 1, SpellSchool.Abjuration, "", "", "", "", "", _diceRoller);
            var spell2 = new Spell("Test2", 1, SpellSchool.Abjuration, "", "", "", "", "", _diceRoller);
            caster.LearnSpell(spell1);
            caster.LearnSpell(spell2);

            caster.PrepareSpell(spell1);

            caster.PreparedSpells.Should().Contain(s => s.Name == "Test1");
            caster.PreparedSpells.Should().NotContain(s => s.Name == "Test2");
        }
    }
}
