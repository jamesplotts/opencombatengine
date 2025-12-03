using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class MagicItemChargesTests
    {
        [Fact]
        public void Item_Should_Start_With_Max_Charges()
        {
            // Arrange
            var item = new MagicItem("Wand", "Desc", 1, 100, ItemType.Wand, true, maxCharges: 7, rechargeRate: "1d6+1");

            // Assert
            item.Charges.Should().Be(7);
            item.MaxCharges.Should().Be(7);
        }

        [Fact]
        public void Consuming_Charges_Should_Reduce_Count()
        {
            // Arrange
            var item = new MagicItem("Wand", "Desc", 1, 100, ItemType.Wand, true, maxCharges: 7);

            // Act
            var result = item.ConsumeCharges(3);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.Charges.Should().Be(4);
        }

        [Fact]
        public void Consuming_Too_Many_Charges_Should_Fail()
        {
            // Arrange
            var item = new MagicItem("Wand", "Desc", 1, 100, ItemType.Wand, true, maxCharges: 7);

            // Act
            var result = item.ConsumeCharges(8);

            // Assert
            result.IsSuccess.Should().BeFalse();
            item.Charges.Should().Be(7);
        }

        [Fact]
        public void Recharging_Should_Increase_Count_Up_To_Max()
        {
            // Arrange
            var item = new MagicItem("Wand", "Desc", 1, 100, ItemType.Wand, true, maxCharges: 7);
            item.ConsumeCharges(5); // 2 left

            // Act
            var result = item.Recharge(3);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.Charges.Should().Be(5);

            // Act 2
            item.Recharge(10); // Should cap at 7

            // Assert 2
            item.Charges.Should().Be(7);
        }

        [Fact]
        public void CastSpellFromItemAction_Should_Consume_Charges_And_Not_Slots()
        {
            // Arrange
            var spell = Substitute.For<ISpell>();
            spell.Name.Returns("Magic Missile");
            spell.Level.Returns(1);
            spell.Cast(Arg.Any<ICreature>(), Arg.Any<ICreature>()).Returns(OpenCombatEngine.Core.Results.Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(new OpenCombatEngine.Core.Models.Spells.SpellResolution(true, "Cast")));

            var item = new MagicItem("Wand of Magic Missiles", "Desc", 1, 500, ItemType.Wand, true, maxCharges: 7);
            
            // Mock creature with spellcasting but NO slots
            var creature = Substitute.For<ICreature>();
            creature.Name.Returns("Wizard");
            var spellcasting = Substitute.For<ISpellCaster>();
            spellcasting.HasSlot(1).Returns(false); // No slots!
            creature.Spellcasting.Returns(spellcasting);
            
            var action = new CastSpellFromItemAction(spell, item, chargesCost: 1);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(creature, new CreatureTarget(creature), null); // Self cast for simplicity

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.Charges.Should().Be(6);
            spellcasting.DidNotReceive().ConsumeSlot(Arg.Any<int>());
        }
    }
}
