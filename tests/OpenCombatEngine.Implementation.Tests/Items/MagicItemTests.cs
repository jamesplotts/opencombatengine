using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Items
{
    public class MagicItemTests
    {
        [Fact]
        public void Attune_Should_Succeed_If_Requirements_Met()
        {
            var creature = Substitute.For<ICreature>();
            var item = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, true);

            var result = item.Attune(creature);

            result.IsSuccess.Should().BeTrue();
            item.AttunedCreature.Should().Be(creature);
        }

        [Fact]
        public void Attune_Should_Fail_If_Not_Required()
        {
            var creature = Substitute.For<ICreature>();
            var item = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, false);

            var result = item.Attune(creature);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("does not require attunement");
        }

        [Fact]
        public void Attune_Should_Fail_If_Already_Attuned()
        {
            var creature1 = Substitute.For<ICreature>();
            var creature2 = Substitute.For<ICreature>();
            var item = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, true);

            item.Attune(creature1);
            var result = item.Attune(creature2);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("already attuned");
        }

        [Fact]
        public void Unattune_Should_Clear_Attunement()
        {
            var creature = Substitute.For<ICreature>();
            var item = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, true);

            item.Attune(creature);
            var result = item.Unattune();

            result.IsSuccess.Should().BeTrue();
            item.AttunedCreature.Should().BeNull();
        }

        [Fact]
        public void Clone_Should_Produce_Independent_Fresh_Instance()
        {
            var creature = Substitute.For<ICreature>();
            var original = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, true, maxCharges: 5);
            original.ConsumeCharges(3);
            original.Attune(creature);

            var clone = original.Clone();

            // Base data carried over.
            clone.Name.Should().Be("Ring");
            clone.Description.Should().Be("A ring");
            clone.RequiresAttunement.Should().BeTrue();
            clone.MaxCharges.Should().Be(5);

            // Mutable state is fresh, not copied - the whole point of cloning.
            clone.Charges.Should().Be(5);
            clone.AttunedCreature.Should().BeNull();

            // Original is untouched by anything done to the clone.
            clone.Attune(Substitute.For<ICreature>());
            original.AttunedCreature.Should().Be(creature);
            original.Charges.Should().Be(2);
        }

        [Fact]
        public void Rarity_Should_Default_To_Common_But_Be_Settable()
        {
            var defaulted = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, true);
            var rare = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, true, rarity: ItemRarity.Rare);

            defaulted.Rarity.Should().Be(ItemRarity.Common);
            rare.Rarity.Should().Be(ItemRarity.Rare);
        }

        [Fact]
        public void Clone_Should_Preserve_Rarity()
        {
            var item = new MagicItem("Ring", "A ring", 0.1, 100, ItemType.Accessory, true, rarity: ItemRarity.Legendary);

            var clone = item.Clone();

            clone.Rarity.Should().Be(ItemRarity.Legendary);
        }
    }
}
