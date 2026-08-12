using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Content.Mappers;
using OpenCombatEngine.Implementation.Open5e.Models;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class Open5eItemMapperTests
    {
        [Fact]
        public void MapMagicItem_Should_Produce_A_Real_Magic_Item_Not_A_Plain_Item()
        {
            var source = new Open5eMagicItem
            {
                Name = "Wand of Magic Missiles",
                Slug = "wand-of-magic-missiles",
                Desc = "This wand has 7 charges. While holding it, you can use an action to " +
                       "expend 1 or more of its charges to cast the magic missile spell from it. " +
                       "The wand regains 1d6 + 1 expended charges daily at dawn.",
                Type = "Wand",
                Rarity = "Uncommon",
                RequiresAttunement = string.Empty
            };

            var item = Open5eItemMapper.MapMagicItem(source);

            item.Should().BeAssignableTo<IMagicItem>();
            var magicItem = (IMagicItem)item;

            magicItem.Name.Should().Be("Wand of Magic Missiles");
            magicItem.Rarity.Should().Be(ItemRarity.Uncommon);
            magicItem.RequiresAttunement.Should().BeFalse();
            magicItem.MaxCharges.Should().Be(7);
            magicItem.Charges.Should().Be(7);
            magicItem.RechargeFrequency.Should().Be(RechargeFrequency.Dawn);
            magicItem.RechargeFormula.Should().Be("1d6+1");
        }

        [Fact]
        public void MapMagicItem_Should_Parse_RequiresAttunement()
        {
            var source = new Open5eMagicItem
            {
                Name = "Cloak of Protection",
                Slug = "cloak-of-protection",
                Desc = "You gain a +1 bonus to AC and saving throws while you wear this cloak.",
                Type = "Wondrous Item",
                Rarity = "Uncommon",
                RequiresAttunement = "requires attunement"
            };

            var item = (IMagicItem)Open5eItemMapper.MapMagicItem(source);

            item.RequiresAttunement.Should().BeTrue();
            item.MaxCharges.Should().Be(0); // No charges mentioned in the description
            item.RechargeFrequency.Should().Be(RechargeFrequency.Unspecified);
        }

        [Fact]
        public void MapMagicItem_Should_Leave_Charges_At_Zero_When_Description_Has_None()
        {
            var source = new Open5eMagicItem
            {
                Name = "Ring of Spell Storing",
                Slug = "ring-of-spell-storing",
                Desc = "This ring stores spells cast into it, holding them until the attuned wearer " +
                       "uses them. The ring can store up to 5 levels worth of spells at a time.",
                Type = "Ring",
                Rarity = "Rare",
                RequiresAttunement = "requires attunement"
            };

            var item = (IMagicItem)Open5eItemMapper.MapMagicItem(source);

            item.MaxCharges.Should().Be(0);
            item.RechargeFormula.Should().BeEmpty();
        }
    }
}
