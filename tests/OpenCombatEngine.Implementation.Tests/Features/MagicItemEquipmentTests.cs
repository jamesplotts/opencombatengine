using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class MagicItemEquipmentTests
    {
        [Fact]
        public void Should_Equip_Magic_Weapon_In_MainHand()
        {
            // Arrange
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            
            var weaponProps = Substitute.For<IWeapon>();
            weaponProps.Name.Returns("Longsword +1");
            
            var magicItem = new MagicItem(
                "Longsword +1", 
                "A magical sword", 
                3, 
                500, 
                ItemType.Weapon, 
                false, 
                weaponProperties: weaponProps
            );

            // Act
            var result = manager.EquipMainHand(magicItem);

            // Assert
            result.IsSuccess.Should().BeTrue();
            manager.MainHand.Should().Be(weaponProps);
        }

        [Fact]
        public void Should_Equip_Magic_Armor()
        {
            // Arrange
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            
            var armorProps = Substitute.For<IArmor>();
            armorProps.Name.Returns("Plate +1");
            armorProps.Category.Returns(ArmorCategory.Heavy);
            
            var magicItem = new MagicItem(
                "Plate +1", 
                "Magical plate armor", 
                65, 
                2000, 
                ItemType.Armor, 
                false, 
                armorProperties: armorProps
            );

            // Act
            var result = manager.EquipArmor(magicItem);

            // Assert
            result.IsSuccess.Should().BeTrue();
            manager.Armor.Should().Be(armorProps);
        }

        [Fact]
        public void Should_Fail_To_Equip_Magic_Item_Without_Properties()
        {
            // Arrange
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            
            var magicItem = new MagicItem(
                "Wondrous Item", 
                "Just an item", 
                1, 
                100, 
                ItemType.WondrousItem, 
                false
            );

            // Act
            var result = manager.EquipMainHand(magicItem);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not a weapon");
        }
    }
}
