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
    public class StandardEquipmentManagerTests
    {
        [Fact]
        public void AttuneItem_Should_Succeed_And_Apply_Bonuses()
        {
            var owner = Substitute.For<ICreature>();
            var conditions = Substitute.For<IConditionManager>();
            owner.Conditions.Returns(conditions);

            var manager = new StandardEquipmentManager(owner);
            
            var feature = Substitute.For<IFeature>();
            var condition = Substitute.For<ICondition>();
            var item = new MagicItem("Ring", "Desc", 1, 100, ItemType.Accessory, true, 
                features: new[] { feature }, 
                conditions: new[] { condition });

            var result = manager.AttuneItem(item);

            result.IsSuccess.Should().BeTrue();
            manager.AttunedItems.Should().Contain(item);
            item.AttunedCreature.Should().Be(owner);
            
            owner.Received().AddFeature(feature);
            conditions.Received().AddCondition(condition);
        }

        [Fact]
        public void AttuneItem_Should_Fail_If_Limit_Reached()
        {
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            
            var item1 = new MagicItem("Item1", "Desc", 1, 100, ItemType.Accessory, true);
            var item2 = new MagicItem("Item2", "Desc", 1, 100, ItemType.Accessory, true);
            var item3 = new MagicItem("Item3", "Desc", 1, 100, ItemType.Accessory, true);
            var item4 = new MagicItem("Item4", "Desc", 1, 100, ItemType.Accessory, true);

            manager.AttuneItem(item1).IsSuccess.Should().BeTrue();
            manager.AttuneItem(item2).IsSuccess.Should().BeTrue();
            manager.AttuneItem(item3).IsSuccess.Should().BeTrue();
            
            var result = manager.AttuneItem(item4);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("slots full");
        }

        [Fact]
        public void UnattuneItem_Should_Remove_Bonuses()
        {
            var owner = Substitute.For<ICreature>();
            var conditions = Substitute.For<IConditionManager>();
            owner.Conditions.Returns(conditions);

            var manager = new StandardEquipmentManager(owner);
            
            var feature = Substitute.For<IFeature>();
            var condition = Substitute.For<ICondition>();
            var item = new MagicItem("Ring", "Desc", 1, 100, ItemType.Accessory, true, 
                features: new[] { feature }, 
                conditions: new[] { condition });

            manager.AttuneItem(item);
            var result = manager.UnattuneItem(item);

            result.IsSuccess.Should().BeTrue();
            manager.AttunedItems.Should().NotContain(item);
            item.AttunedCreature.Should().BeNull();
            
            owner.Received().RemoveFeature(feature);
            conditions.Received().RemoveCondition(condition.Name);
        }

        [Fact]
        public void EquipOffHand_Should_Unequip_Same_Item_From_MainHand()
        {
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);

            manager.EquipMainHand(sword);
            manager.EquipOffHand(sword);

            manager.MainHand.Should().BeNull();
            manager.OffHand.Should().Be(sword);
            manager.GetEquippedItems().Should().ContainSingle();
        }

        [Fact]
        public void Equip_HandSlotOccupiedByDifferentItem_Rejects()
        {
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);
            var axe = new Weapon("Handaxe", "1d6", DamageType.Slashing);
            manager.EquipMainHand(sword);

            var result = manager.Equip(axe, EquipmentSlot.MainHand);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("already occupied");
            manager.MainHand.Should().Be(sword);
        }

        [Fact]
        public void Equip_HandSlotOccupiedBySameItem_Idempotent()
        {
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);
            manager.EquipMainHand(sword);

            var result = manager.Equip(sword, EquipmentSlot.MainHand);

            result.IsSuccess.Should().BeTrue();
            manager.MainHand.Should().Be(sword);
        }

        [Fact]
        public void Equip_HandSlotFreedByUnequip_ThenAcceptsNewItem()
        {
            // The "drop it (free)" half of the hands-full rule: UnequipItem
            // already has no action-economy cost anywhere, so freeing a hand
            // this way and then equipping something new must just work.
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);
            var axe = new Weapon("Handaxe", "1d6", DamageType.Slashing);
            manager.EquipMainHand(sword);

            manager.Unequip(EquipmentSlot.MainHand);
            var result = manager.Equip(axe, EquipmentSlot.MainHand);

            result.IsSuccess.Should().BeTrue();
            manager.MainHand.Should().Be(axe);
        }

        [Fact]
        public void Equip_ArmorSlotOccupied_StillSwapsSilently()
        {
            // Confirms the hand-slot gate is scoped to MainHand/OffHand only —
            // Armor and the other non-hand slots keep their original silent-
            // swap ergonomics, deliberately unchanged by this feature.
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var leather = new Armor("Leather", 11, ArmorCategory.Light);
            var studded = new Armor("Studded Leather", 12, ArmorCategory.Light);
            manager.EquipArmor(leather);

            var result = manager.Equip(studded, EquipmentSlot.Armor);

            result.IsSuccess.Should().BeTrue();
            manager.Armor.Should().Be(studded);
        }

        [Fact]
        public void GetSlotFor_EquippedItem_ReturnsItsSlot()
        {
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);
            manager.EquipMainHand(sword);

            manager.GetSlotFor(sword).Should().Be(EquipmentSlot.MainHand);
        }

        [Fact]
        public void GetSlotFor_UnequippedItem_ReturnsNull()
        {
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var sword = new Weapon("Longsword", "1d8", DamageType.Slashing);

            manager.GetSlotFor(sword).Should().BeNull();
        }

        [Fact]
        public void Equip_Back_Succeeds()
        {
            var owner = Substitute.For<ICreature>();
            var manager = new StandardEquipmentManager(owner);
            var pack = new MagicItem("Explorer's Pack", "Desc", 1, 10, ItemType.Accessory, false);

            var result = manager.Equip(pack, EquipmentSlot.Back);

            result.IsSuccess.Should().BeTrue();
            manager.Back.Should().Be(pack);
            manager.GetSlotFor(pack).Should().Be(EquipmentSlot.Back);
        }
    }
}
