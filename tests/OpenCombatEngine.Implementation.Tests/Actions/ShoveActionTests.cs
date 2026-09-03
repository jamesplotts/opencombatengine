// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    public class ShoveActionTests
    {
        private readonly IDiceRoller _diceRoller = Substitute.For<IDiceRoller>();

        private static ICreature MakeCreature(int athletics, int acrobatics, bool hasAction = true)
        {
            var creature = Substitute.For<ICreature>();
            var checks = Substitute.For<ICheckManager>();
            checks.RollAbilityCheck(Ability.Strength, "Athletics").Returns(Result<DiceRollResult>.Success(new DiceRollResult(athletics, "1d20", new List<int> { athletics }, 0, RollType.Normal)));
            checks.RollAbilityCheck(Ability.Dexterity, "Acrobatics").Returns(Result<DiceRollResult>.Success(new DiceRollResult(acrobatics, "1d20", new List<int> { acrobatics }, 0, RollType.Normal)));
            creature.Checks.Returns(checks);

            var economy = Substitute.For<IActionEconomy>();
            economy.HasAction.Returns(hasAction);
            creature.ActionEconomy.Returns(economy);

            var conditions = Substitute.For<IConditionManager>();
            creature.Conditions.Returns(conditions);

            return creature;
        }

        [Fact]
        public void Execute_Prone_SourceWins_AppliesProneCondition()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);

            var action = new ShoveAction(_diceRoller, ShoveEffect.Prone);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            result.Value.Success.Should().BeTrue();
            target.Conditions.Received(1).AddCondition(Arg.Is<ICondition>(c => c.Type == ConditionType.Prone));
        }

        [Fact]
        public void Execute_Push_SourceWins_MovesTargetAwayFromSource()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);

            var grid = Substitute.For<IGridManager>();
            var sourcePos = new Position(0, 0);
            var targetPos = new Position(1, 0); // adjacent, 5 feet east
            grid.GetPosition(source).Returns(sourcePos);
            grid.GetPosition(target).Returns(targetPos);
            grid.GetDistance(sourcePos, targetPos).Returns(5);
            grid.HasLineOfSight(sourcePos, targetPos).Returns(true);

            var action = new ShoveAction(_diceRoller, ShoveEffect.Push);
            var context = new StandardActionContext(source, new CreatureTarget(target), grid);

            var result = action.Execute(context);

            result.Value.Success.Should().BeTrue();
            // Pushed one further cell east (away from source).
            grid.Received(1).MoveCreature(target, new Position(2, 0));
            target.Conditions.DidNotReceive().AddCondition(Arg.Any<ICondition>());
        }

        [Fact]
        public void Execute_Push_NoGrid_ResolvesContestWithNoSpatialEffect()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);

            var action = new ShoveAction(_diceRoller, ShoveEffect.Push);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            result.Value.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_SourceLosesOpposedCheck_NoEffectApplied()
        {
            var source = MakeCreature(athletics: 5, acrobatics: 0);
            var target = MakeCreature(athletics: 18, acrobatics: 8);

            var action = new ShoveAction(_diceRoller, ShoveEffect.Prone);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.Value.Success.Should().BeFalse();
            target.Conditions.DidNotReceive().AddCondition(Arg.Any<ICondition>());
        }

        [Fact]
        public void Execute_SourceParalyzed_CannotAct()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);
            source.Conditions.HasCondition(ConditionType.Paralyzed).Returns(true);

            var action = new ShoveAction(_diceRoller, ShoveEffect.Prone);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Paralyzed");
        }
    }
}
