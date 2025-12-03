using System;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Items
{
    public class MagicItemRechargeTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly MagicItemRecharger _recharger;
        private readonly ICreature _creature;

        public MagicItemRechargeTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            _recharger = new MagicItemRecharger(_diceRoller);
            _creature = Substitute.For<ICreature>();
        }

        [Fact]
        public void RechargeItems_Should_Recharge_Items_With_Matching_Frequency()
        {
            // Arrange
            var item = Substitute.For<IMagicItem>();
            item.RechargeFrequency.Returns(RechargeFrequency.Dawn);
            item.RechargeFormula.Returns("1d6+1");
            item.Charges.Returns(0);
            item.MaxCharges.Returns(10);
            
            _creature.Inventory.Items.Returns(new[] { item });

            _diceRoller.Roll("1d6+1").Returns(Result<DiceRollResult>.Success(new DiceRollResult(5, "1d6+1", new[] { 4 }, 1, RollType.Normal))); // Total 5

            // Act
            var result = _recharger.RechargeItems(_creature, RechargeFrequency.Dawn);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(1); // 1 item recharged
            item.Received().Recharge(5);
        }

        [Fact]
        public void RechargeItems_Should_Ignore_Items_With_Different_Frequency()
        {
            // Arrange
            var item = Substitute.For<IMagicItem>();
            item.RechargeFrequency.Returns(RechargeFrequency.Dusk);
            
            _creature.Inventory.Items.Returns(new[] { item });

            // Act
            var result = _recharger.RechargeItems(_creature, RechargeFrequency.Dawn);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
            item.DidNotReceive().Recharge(Arg.Any<int>());
        }
    }
}
