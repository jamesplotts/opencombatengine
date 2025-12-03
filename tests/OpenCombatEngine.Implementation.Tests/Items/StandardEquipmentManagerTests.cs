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
    }
}
