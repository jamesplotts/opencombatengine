using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;
using System.Collections.Generic;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ConditionEffectsTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly StandardCreature _attacker;
        private readonly StandardCreature _target;

        public ConditionEffectsTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            
            // Setup Attacker
            var attackerStats = Substitute.For<IAbilityScores>();
            attackerStats.GetModifier(Arg.Any<Ability>()).Returns(0);
            _attacker = new StandardCreature("11111111-1111-1111-1111-111111111111", "Attacker", attackerStats, Substitute.For<IHitPoints>(), new StandardInventory(), new StandardTurnManager(_diceRoller));

            // Setup Target
            var targetStats = Substitute.For<IAbilityScores>();
            targetStats.GetModifier(Arg.Any<Ability>()).Returns(0);
            _target = new StandardCreature("22222222-2222-2222-2222-222222222222", "Target", targetStats, Substitute.For<IHitPoints>(), new StandardInventory(), new StandardTurnManager(_diceRoller));
        }

        [Fact]
        public void Blinded_Should_Impose_Disadvantage_On_Attacker()
        {
            // Arrange
            _attacker.Conditions.AddCondition(ConditionFactory.Create(ConditionType.Blinded));
            
            var action = new AttackAction("Test Attack", "Desc", 0, "1d6", DamageType.Slashing, 0, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(_attacker, new CreatureTarget(_target), null);

            // Mock Dice
            _diceRoller.RollWithDisadvantage(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Disadvantage)));
            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(4, "1d6", new List<int> { 4 }, 0, RollType.Normal)));

            // Act
            action.Execute(context);

            // Assert
            _diceRoller.Received(1).RollWithDisadvantage(Arg.Any<string>());
        }

        [Fact]
        public void Blinded_Target_Should_Grant_Advantage_To_Attacker()
        {
            // Arrange
            _target.Conditions.AddCondition(ConditionFactory.Create(ConditionType.Blinded));
            
            var action = new AttackAction("Test Attack", "Desc", 0, "1d6", DamageType.Slashing, 0, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(_attacker, new CreatureTarget(_target), null);

            // Mock Dice
            _diceRoller.RollWithAdvantage(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Advantage)));
            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(4, "1d6", new List<int> { 4 }, 0, RollType.Normal)));

            // Act
            action.Execute(context);

            // Assert
            _diceRoller.Received(1).RollWithAdvantage(Arg.Any<string>());
        }

        [Fact]
        public void Poisoned_Should_Impose_Disadvantage_On_Attacker()
        {
            // Arrange
            _attacker.Conditions.AddCondition(ConditionFactory.Create(ConditionType.Poisoned));
            
            var action = new AttackAction("Test Attack", "Desc", 0, "1d6", DamageType.Slashing, 0, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(_attacker, new CreatureTarget(_target), null);

            // Mock Dice
            _diceRoller.RollWithDisadvantage(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Disadvantage)));
            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(4, "1d6", new List<int> { 4 }, 0, RollType.Normal)));

            // Act
            action.Execute(context);

            // Assert
            _diceRoller.Received(1).RollWithDisadvantage(Arg.Any<string>());
        }

        [Fact]
        public void Restrained_Should_Impose_Disadvantage_On_Attacker()
        {
            _attacker.Conditions.AddCondition(ConditionFactory.Create(ConditionType.Restrained));
            
            var action = new AttackAction("Test Attack", "Desc", 0, "1d6", DamageType.Slashing, 0, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(_attacker, new CreatureTarget(_target), null);

            _diceRoller.RollWithDisadvantage(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Disadvantage)));
            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(4, "1d6", new List<int> { 4 }, 0, RollType.Normal)));

            action.Execute(context);
            _diceRoller.Received(1).RollWithDisadvantage(Arg.Any<string>());
        }

        [Fact]
        public void Restrained_Target_Should_Grant_Advantage_To_Attacker()
        {
            _target.Conditions.AddCondition(ConditionFactory.Create(ConditionType.Restrained));
            
            var action = new AttackAction("Test Attack", "Desc", 0, "1d6", DamageType.Slashing, 0, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(_attacker, new CreatureTarget(_target), null);

            _diceRoller.RollWithAdvantage(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Advantage)));
            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(4, "1d6", new List<int> { 4 }, 0, RollType.Normal)));

            action.Execute(context);
            _diceRoller.Received(1).RollWithAdvantage(Arg.Any<string>());
        }
    }
}
