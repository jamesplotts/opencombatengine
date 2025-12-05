using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Items
{
    public class MagicItemActiveTests
    {
        private readonly StandardCreature _creature;

        public MagicItemActiveTests()
        {
            _creature = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Test User",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );
        }

        [Fact]
        public void UseMagicItemAction_Should_Consume_Charges()
        {
            // Arrange
            bool executed = false;
            var ability = new MagicItemAbility(
                "Test Ability",
                "Description",
                cost: 2,
                ActionType.Action,
                (user, ctx) => { executed = true; return Result<bool>.Success(true); }
            );

            var item = new MagicItem(
                "Wand",
                "A wand",
                1,
                100,
                ItemType.WondrousItem,
                false,
                maxCharges: 5,
                abilities: new[] { ability }
            );

            var action = new UseMagicItemAction(item, ability);

            // Act
            var context = Substitute.For<IActionContext>();
            context.Source.Returns(_creature);
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            executed.Should().BeTrue();
            item.Charges.Should().Be(3); // 5 - 2
        }

        [Fact]
        public void UseMagicItemAction_Should_Fail_If_Not_Enough_Charges()
        {
            // Arrange
            var ability = new MagicItemAbility(
                "Test Ability",
                "Description",
                cost: 6,
                ActionType.Action,
                (user, ctx) => Result<bool>.Success(true)
            );

            var item = new MagicItem(
                "Wand",
                "A wand",
                1,
                100,
                ItemType.WondrousItem,
                false,
                maxCharges: 5,
                abilities: new[] { ability }
            );

            var action = new UseMagicItemAction(item, ability);

            // Act
            var context = Substitute.For<IActionContext>();
            context.Source.Returns(_creature);
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Not enough charges");
            item.Charges.Should().Be(5); // No change
        }

        [Fact]
        public void Creature_Should_Have_Magic_Item_Actions_When_Equipped()
        {
            // Arrange
            var ability = new MagicItemAbility(
                "Fireball",
                "Cast Fireball",
                cost: 1,
                ActionType.Action,
                (user, ctx) => Result<bool>.Success(true)
            );

            var item = new MagicItem(
                "Wand of Fireballs",
                "Boom",
                1,
                500,
                ItemType.WondrousItem,
                false,
                maxCharges: 7,
                abilities: new[] { ability },
                defaultSlot: EquipmentSlot.MainHand,
                weaponProperties: new OpenCombatEngine.Implementation.Items.Weapon("Wand", "1d4", DamageType.Bludgeoning, weight: 1, value: 10)
            );

            _creature.Inventory.AddItem(item);
            var equipResult = _creature.Equipment.Equip(item, EquipmentSlot.MainHand);
            equipResult.IsSuccess.Should().BeTrue("Item should be equipped successfully");

            // Act
            var actions = _creature.Actions.ToList();

            // Assert
            actions.Should().Contain(a => a is UseMagicItemAction && a.Name.Contains("Fireball"));
        }
    }
}
