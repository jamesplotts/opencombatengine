using FluentAssertions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class ActionEconomyTests
    {
        [Fact]
        public void Should_Start_With_All_Actions_Available()
        {
            var economy = new StandardActionEconomy();
            economy.HasAction.Should().BeTrue();
            economy.HasBonusAction.Should().BeTrue();
            economy.HasReaction.Should().BeTrue();
        }

        [Fact]
        public void UseAction_Should_Consume_Action()
        {
            var economy = new StandardActionEconomy();
            economy.UseAction();
            economy.HasAction.Should().BeFalse();
        }

        [Fact]
        public void UseBonusAction_Should_Consume_BonusAction()
        {
            var economy = new StandardActionEconomy();
            economy.UseBonusAction();
            economy.HasBonusAction.Should().BeFalse();
        }

        [Fact]
        public void UseReaction_Should_Consume_Reaction()
        {
            var economy = new StandardActionEconomy();
            economy.UseReaction();
            economy.HasReaction.Should().BeFalse();
        }

        [Fact]
        public void ResetTurn_Should_Restore_All_Actions()
        {
            var economy = new StandardActionEconomy();
            economy.UseAction();
            economy.UseBonusAction();
            economy.UseReaction();

            economy.ResetTurn();

            economy.HasAction.Should().BeTrue();
            economy.HasBonusAction.Should().BeTrue();
            economy.HasReaction.Should().BeTrue();
        }
        
        [Fact]
        public void StandardCreature_StartTurn_Should_Reset_Economy()
        {
            var creature = new StandardCreature("Hero", new StandardAbilityScores(), new StandardHitPoints(10));
            creature.ActionEconomy.UseAction();
            
            creature.StartTurn();
            
            creature.ActionEconomy.HasAction.Should().BeTrue();
        }
    }
}
