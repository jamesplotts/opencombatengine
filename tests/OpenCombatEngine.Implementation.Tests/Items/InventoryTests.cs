using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Items
{
    public class InventoryTests
    {
        [Fact]
        public void Should_Add_And_Remove_Items()
        {
            var inventory = new StandardInventory();
            var item = new Item("Potion");

            inventory.AddItem(item).IsSuccess.Should().BeTrue();
            inventory.Items.Should().Contain(item);

            inventory.RemoveItem(item).IsSuccess.Should().BeTrue();
            inventory.Items.Should().NotContain(item);
        }

        [Fact]
        public void Should_Get_Item_By_Name()
        {
            var inventory = new StandardInventory();
            var item = new Item("Sword");
            inventory.AddItem(item);

            inventory.GetItem("Sword").Should().Be(item);
            inventory.GetItem("Shield").Should().BeNull();
        }
    }

    public class EquipmentTests
    {
        [Fact]
        public void Should_Equip_And_Unequip_Weapon()
        {
            var equipment = new StandardEquipmentManager();
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);

            equipment.EquipMainHand(sword).IsSuccess.Should().BeTrue();
            equipment.MainHand.Should().Be(sword);

            equipment.UnequipMainHand();
            equipment.MainHand.Should().BeNull();
        }

        [Fact]
        public void Should_Equip_And_Unequip_Armor()
        {
            var equipment = new StandardEquipmentManager();
            var mail = new Armor("Chain Mail", 16, ArmorCategory.Heavy);

            equipment.EquipArmor(mail).IsSuccess.Should().BeTrue();
            equipment.Armor.Should().Be(mail);

            equipment.UnequipArmor();
            equipment.Armor.Should().BeNull();
        }

        [Fact]
        public void Should_Not_Equip_Shield_As_Armor()
        {
            var equipment = new StandardEquipmentManager();
            var shield = new Armor("Shield", 2, ArmorCategory.Shield);

            equipment.EquipArmor(shield).IsSuccess.Should().BeFalse();
        }
    }
}
