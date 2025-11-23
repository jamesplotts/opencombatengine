using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    public class CastSpellActionTests
    {
        [Fact]
        public void Execute_Should_Fail_If_No_Spellcasting()
        {
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", (c, t) => Result<bool>.Success(true));
            var action = new CastSpellAction(spell);
            var creature = Substitute.For<ICreature>();
            creature.Spellcasting.Returns((ISpellCaster?)null);

            var result = action.Execute(creature, Substitute.For<ICreature>());

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("cannot cast spells");
        }

        [Fact]
        public void Execute_Should_Fail_If_No_Slots()
        {
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", (c, t) => Result<bool>.Success(true));
            var action = new CastSpellAction(spell);
            
            var spellCaster = Substitute.For<ISpellCaster>();
            spellCaster.HasSlot(1).Returns(false);
            spellCaster.PreparedSpells.Returns(new[] { spell }); // Mock prepared

            var creature = Substitute.For<ICreature>();
            creature.Spellcasting.Returns(spellCaster);

            var result = action.Execute(creature, Substitute.For<ICreature>());

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("No spell slots");
        }

        [Fact]
        public void Execute_Should_Consume_Slot_And_Cast_Spell()
        {
            bool casted = false;
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", (c, t) => 
            {
                casted = true;
                return Result<bool>.Success(true);
            });
            var action = new CastSpellAction(spell);
            
            var spellCaster = Substitute.For<ISpellCaster>();
            spellCaster.HasSlot(1).Returns(true);
            spellCaster.ConsumeSlot(1).Returns(Result<bool>.Success(true));
            spellCaster.PreparedSpells.Returns(new[] { spell }); // Mock prepared

            var creature = Substitute.For<ICreature>();
            creature.Spellcasting.Returns(spellCaster);

            var result = action.Execute(creature, Substitute.For<ICreature>());

            result.IsSuccess.Should().BeTrue();
            casted.Should().BeTrue();
            spellCaster.Received().ConsumeSlot(1);
        }

        [Fact]
        public void Execute_Should_Fail_If_Not_Prepared()
        {
            var spell = new Spell("Test", 1, SpellSchool.Abjuration, "", "", "", "", "", (c, t) => Result<bool>.Success(true));
            var action = new CastSpellAction(spell);

            var spellCaster = Substitute.For<ISpellCaster>();
            spellCaster.PreparedSpells.Returns(new System.Collections.Generic.List<ISpell>()); // Empty prepared list

            var creature = Substitute.For<ICreature>();
            creature.Spellcasting.Returns(spellCaster);

            var result = action.Execute(creature, Substitute.For<ICreature>());

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not prepared");
        }
    }
}
