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
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    public class GrappleActionTests
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

            // NSubstitute auto-recursively-mocks an unconfigured
            // interface-returning property to a non-null substitute
            // rather than null — MainHand/OffHand must be stubbed to
            // null explicitly, or the free-hand check below would always
            // see them as "occupied."
            var equipment = Substitute.For<IEquipmentManager>();
            equipment.MainHand.Returns((IWeapon?)null);
            equipment.OffHand.Returns((IWeapon?)null);
            equipment.Shield.Returns((IArmor?)null);
            creature.Equipment.Returns(equipment);

            return creature;
        }

        [Fact]
        public void Execute_SourceWinsOpposedCheck_AppliesGrappledCondition()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);

            var action = new GrappleAction(_diceRoller);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            result.Value.Success.Should().BeTrue();
            target.Conditions.Received(1).AddCondition(Arg.Is<ICondition>(c => c.Type == ConditionType.Grappled));
        }

        [Fact]
        public void Execute_SourceLosesOpposedCheck_DoesNotApplyCondition()
        {
            var source = MakeCreature(athletics: 5, acrobatics: 0);
            var target = MakeCreature(athletics: 18, acrobatics: 8);

            var action = new GrappleAction(_diceRoller);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            result.Value.Success.Should().BeFalse();
            target.Conditions.DidNotReceive().AddCondition(Arg.Any<ICondition>());
        }

        [Fact]
        public void Execute_TargetUsesBetterOfAthleticsOrAcrobatics()
        {
            // Target's Athletics is weak but Acrobatics is strong enough
            // to beat the source — the contest must use the better of
            // the two, not just Athletics.
            var source = MakeCreature(athletics: 12, acrobatics: 0);
            var target = MakeCreature(athletics: 1, acrobatics: 15);

            var action = new GrappleAction(_diceRoller);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.Value.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_NoOffHandFree_TwoHandedMainHand_Fails()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);

            var twoHander = Substitute.For<IWeapon>();
            twoHander.Properties.Returns(new[] { WeaponProperty.TwoHanded });
            source.Equipment.MainHand.Returns(twoHander);

            var action = new GrappleAction(_diceRoller);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("no hand free");
        }

        [Fact]
        public void Execute_OffHandOccupied_Fails()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);
            source.Equipment.OffHand.Returns(Substitute.For<IWeapon>());

            var action = new GrappleAction(_diceRoller);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("no hand free");
        }

        [Fact]
        public void Execute_NoActionAvailable_Fails()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0, hasAction: false);
            var target = MakeCreature(athletics: 10, acrobatics: 8);

            var action = new GrappleAction(_diceRoller);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Resource already used");
        }

        [Fact]
        public void Execute_SourceParalyzed_CannotAct()
        {
            var source = MakeCreature(athletics: 18, acrobatics: 0);
            var target = MakeCreature(athletics: 10, acrobatics: 8);
            source.Conditions.HasCondition(ConditionType.Paralyzed).Returns(true);

            var action = new GrappleAction(_diceRoller);
            var context = new StandardActionContext(source, new CreatureTarget(target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Paralyzed");
        }
    }
}
