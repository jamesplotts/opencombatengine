using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class AccessorySlotsTests
    {
        [Fact]
        public void Should_Equip_Boots_To_Feet()
        {
            // Arrange
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            
            var boots = new MagicItem(
                "Boots of Speed", 
                "Fast boots", 
                2, 
                500, 
                ItemType.WondrousItem, 
                false,
                defaultSlot: EquipmentSlot.Feet
            );

            // Act
            var result = manager.Equip(boots, EquipmentSlot.Feet);

            // Assert
            result.IsSuccess.Should().BeTrue();
            manager.Feet.Should().Be(boots);
        }

        [Fact]
        public void Should_Equip_Rings_To_Ring_Slots()
        {
            // Arrange
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            
            var ring1 = new MagicItem("Ring of Protection", "Prot", 0, 1000, ItemType.Ring, true, defaultSlot: EquipmentSlot.Ring1);
            var ring2 = new MagicItem("Ring of Fire Resistance", "Fire", 0, 1000, ItemType.Ring, true, defaultSlot: EquipmentSlot.Ring2);

            // Act
            manager.Equip(ring1, EquipmentSlot.Ring1);
            manager.Equip(ring2, EquipmentSlot.Ring2);

            // Assert
            manager.Ring1.Should().Be(ring1);
            manager.Ring2.Should().Be(ring2);
        }

        [Fact]
        public void Should_Unequip_Accessory()
        {
            // Arrange
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var helm = new MagicItem("Helm of Brilliance", "Shiny", 5, 5000, ItemType.WondrousItem, true, defaultSlot: EquipmentSlot.Head);
            manager.Equip(helm, EquipmentSlot.Head);

            // Act
            var result = manager.Unequip(EquipmentSlot.Head);

            // Assert
            result.IsSuccess.Should().BeTrue();
            manager.Head.Should().BeNull();
        }
    }
}
