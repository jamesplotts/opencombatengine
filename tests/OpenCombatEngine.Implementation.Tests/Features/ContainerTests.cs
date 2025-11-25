using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ContainerTests
    {
        [Fact]
        public void Should_Calculate_Weight_Correctly_For_Normal_Container()
        {
            // Arrange
            var backpack = new ContainerItem("Backpack", 5, 30, 1.0);
            var item1 = new Item("Rope", "Heavy rope", 10, 1);
            var item2 = new Item("Rations", "Food", 2, 5);

            // Act
            backpack.AddItem(item1);
            backpack.AddItem(item2);

            // Assert
            // Base 5 + Contents (10 + 2) = 17
            backpack.Weight.Should().Be(17);
        }

        [Fact]
        public void Should_Calculate_Weight_Correctly_For_BagOfHolding()
        {
            // Arrange
            // Bag of Holding: 15 lbs base, 0 multiplier
            var bag = new ContainerItem("Bag of Holding", 15, 500, 0.0);
            var heavyItem = new Item("Gold Bars", "Very heavy", 100, 5000);

            // Act
            bag.AddItem(heavyItem);

            // Assert
            // Base 15 + Contents (100 * 0) = 15
            bag.Weight.Should().Be(15);
        }

        [Fact]
        public void Should_Enforce_Capacity()
        {
            // Arrange
            var sack = new ContainerItem("Sack", 0.5, 30, 1.0);
            var heavyItem = new Item("Anvil", "Too heavy", 50, 100);

            // Act
            var result = sack.AddItem(heavyItem);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("too heavy");
        }

        [Fact]
        public void Should_Work_With_MagicItem_Composition()
        {
            // Arrange
            var containerProps = new ContainerItem("Bag of Holding", 15, 500, 0.0);
            var magicBag = new MagicItem(
                "Bag of Holding", 
                "Magical bag", 
                15, 
                4000, 
                ItemType.WondrousItem, 
                false,
                containerProperties: containerProps
            );

            // Act
            var item = new Item("Sword", "Sharp", 3, 10);
            var result = magicBag.ContainerProperties.AddItem(item);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // MagicItem weight is static (defined in constructor), but for containers, 
            // the MagicItem might need to delegate Weight property to ContainerProperties?
            // Currently MagicItem.Weight is a simple property.
            // If we want MagicItem to reflect container weight, we might need to change MagicItem.Weight to be virtual or check properties.
            
            // Let's check the ContainerProperties weight directly for now.
            magicBag.ContainerProperties.Weight.Should().Be(15);
        }
    }
}
