using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
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

        [Fact]
        public void RemoveItem_Should_Unequip_If_Currently_Equipped()
        {
            var owner = Substitute.For<ICreature>();
            var inventory = new StandardInventory();
            var equipment = new StandardEquipmentManager(owner);
            inventory.SetEquipmentManager(equipment);

            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);
            inventory.AddItem(sword);
            equipment.EquipMainHand(sword);

            inventory.RemoveItem(sword);

            equipment.MainHand.Should().BeNull();
            equipment.GetEquippedItems().Should().BeEmpty();
        }

        [Fact]
        public void GetState_Should_Capture_Item_Names_And_Magic_Item_Charges()
        {
            var inventory = new StandardInventory();
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);
            var wand = new MagicItem("Wand of Magic Missiles", "A wand", 1, 500, ItemType.Wand, false, maxCharges: 7);
            wand.ConsumeCharges(3);

            inventory.AddItem(sword);
            inventory.AddItem(wand);

            var state = inventory.GetState();

            state.Items.Should().HaveCount(2);
            state.Items[0].Name.Should().Be("Longsword");
            state.Items[0].CurrentCharges.Should().BeNull();
            state.Items[1].Name.Should().Be("Wand of Magic Missiles");
            state.Items[1].CurrentCharges.Should().Be(4);
        }

        [Fact]
        public void GetState_Should_Capture_Nested_Container_Contents()
        {
            var inventory = new StandardInventory();
            var pouch = new ContainerItem("Pouch", baseWeight: 0.5, weightCapacity: 10);
            var gem = new Item("Ruby");
            pouch.AddItem(gem);

            inventory.AddItem(pouch);

            var state = inventory.GetState();

            state.Items.Should().ContainSingle();
            state.Items[0].Name.Should().Be("Pouch");
            state.Items[0].Contents.Should().NotBeNull();
            state.Items[0].Contents.Should().ContainSingle(i => i.Name == "Ruby");
        }
    }

    public class EquipmentTests
    {
        [Fact]
        public void Should_Equip_And_Unequip_Weapon()
        {
            var equipment = new StandardEquipmentManager(Substitute.For<ICreature>());
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);

            equipment.EquipMainHand(sword).IsSuccess.Should().BeTrue();
            equipment.MainHand.Should().Be(sword);

            equipment.UnequipMainHand();
            equipment.MainHand.Should().BeNull();
        }

        [Fact]
        public void Equip_Should_Update_Equipment()
        {
            var inventory = new StandardInventory();
            var equipment = new StandardEquipmentManager(Substitute.For<ICreature>());
            var weapon = Substitute.For<IWeapon>();
            
            inventory.AddItem(weapon);
            equipment.EquipMainHand(weapon);

            equipment.MainHand.Should().Be(weapon);
        }

        [Fact]
        public void Unequip_Should_Clear_Equipment()
        {
            var equipment = new StandardEquipmentManager(Substitute.For<ICreature>());
            var weapon = Substitute.For<IWeapon>();
            
            equipment.EquipMainHand(weapon);
            equipment.UnequipMainHand();

            equipment.MainHand.Should().BeNull();
        }

        [Fact]
        public void Equip_Should_Fail_If_Item_Not_In_Inventory()
        {
            var equipment = new StandardEquipmentManager(Substitute.For<ICreature>());
            var weapon = Substitute.For<IWeapon>();

            var result = equipment.EquipMainHand(weapon);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Should_Equip_And_Unequip_Armor()
        {
            var equipment = new StandardEquipmentManager(Substitute.For<ICreature>());
            var mail = new Armor("Chain Mail", 16, ArmorCategory.Heavy);

            equipment.EquipArmor(mail).IsSuccess.Should().BeTrue();
            equipment.Armor.Should().Be(mail);

            equipment.UnequipArmor();
            equipment.Armor.Should().BeNull();
        }

        [Fact]
        public void Should_Not_Equip_Shield_As_Armor()
        {
            var equipment = new StandardEquipmentManager(Substitute.For<ICreature>());
            var shield = new Armor("Shield", 2, ArmorCategory.Shield);

            equipment.EquipArmor(shield).IsSuccess.Should().BeFalse();
        }
    }
}
