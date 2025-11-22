using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class SpellCasterTests
    {
        [Fact]
        public void Should_Track_Slots()
        {
            var caster = new StandardSpellCaster();
            caster.SetSlots(1, 2);

            caster.GetMaxSlots(1).Should().Be(2);
            caster.GetSlots(1).Should().Be(2);
            caster.HasSlot(1).Should().BeTrue();
        }

        [Fact]
        public void ConsumeSlot_Should_Reduce_Slots()
        {
            var caster = new StandardSpellCaster();
            caster.SetSlots(1, 2);

            var result = caster.ConsumeSlot(1);

            result.IsSuccess.Should().BeTrue();
            caster.GetSlots(1).Should().Be(1);
        }

        [Fact]
        public void ConsumeSlot_Should_Fail_If_No_Slots()
        {
            var caster = new StandardSpellCaster();
            caster.SetSlots(1, 0);

            var result = caster.ConsumeSlot(1);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("No spell slots");
        }

        [Fact]
        public void Cantrips_Should_Not_Consume_Slots()
        {
            var caster = new StandardSpellCaster();
            var result = caster.ConsumeSlot(0);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void RestoreAllSlots_Should_Reset_To_Max()
        {
            var caster = new StandardSpellCaster();
            caster.SetSlots(1, 2);
            caster.ConsumeSlot(1);
            caster.GetSlots(1).Should().Be(1);

            caster.RestoreAllSlots();

            caster.GetSlots(1).Should().Be(2);
        }

        [Fact]
        public void LearnSpell_Should_Add_To_Known()
        {
            var caster = new StandardSpellCaster();
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", (c, t) => Result<bool>.Success(true));

            caster.LearnSpell(spell);

            caster.KnownSpells.Should().Contain(s => s.Name == "Test");
        }
    }
}
