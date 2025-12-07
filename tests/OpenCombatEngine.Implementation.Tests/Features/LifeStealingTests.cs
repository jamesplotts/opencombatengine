using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Features;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation; // For StandardTurnManager
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class LifeStealingTests
    {
        private readonly StandardCreature _attacker;
        private readonly StandardCreature _target;

        public LifeStealingTests()
        {
            _attacker = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Attacker",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(20), // Max 20
                new StandardInventory(),
                new StandardTurnManager(new OpenCombatEngine.Implementation.Dice.StandardDiceRoller())
            );
            
            // Damage the attacker so we can see healing
            _attacker.HitPoints.TakeDamage(15, OpenCombatEngine.Core.Enums.DamageType.Slashing);
            // Current HP: 5

            _target = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Target",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new OpenCombatEngine.Implementation.Dice.StandardDiceRoller())
            );
        }

        [Fact]
        public void LifeStealing_Should_Heal_On_Critical_Hit()
        {
            // Arrange
            var feature = new LifeStealingFeature();
            _attacker.AddFeature(feature);

            // We need to simulate an action that results in a Critical Hit.
            // Since we can't easily force a Crit via standard AttackAction without mocking dice indefinitely,
            // we can simulate the EVENT triggering directly, or use a MockAction.
            
            // Let's fire the event manually on the attacker to verify the LISTENER works.
            // Current implementation of StandardCreature doesn't expose a way to fire ActionEnded externally easily
            // except via PerformAction.
            
            // Creating a Fake Action that returns a Critical result.
            var fakeAction = new FakeCriticalAction();
            var context = new StandardActionContext(_attacker, new CreatureTarget(_target));
            
            // Act
            _attacker.PerformAction(fakeAction, context);

            // Assert
            // Started at 5 HP. Heal 10 -> 15 HP.
            _attacker.HitPoints.Current.Should().Be(15);
        }

        [Fact]
        public void LifeStealing_Should_NOT_Heal_On_Normal_Hit()
        {
             // Arrange
            var feature = new LifeStealingFeature();
            _attacker.AddFeature(feature);
            
            var fakeAction = new FakeNormalAction();
            var context = new StandardActionContext(_attacker, new CreatureTarget(_target));

            // Act
            _attacker.PerformAction(fakeAction, context);

            // Assert
            _attacker.HitPoints.Current.Should().Be(5);
        }

        private class FakeCriticalAction : OpenCombatEngine.Core.Interfaces.Actions.IAction
        {
            public string Name => "Fake Crit";
            public string Description => "Always crits";
            public OpenCombatEngine.Core.Enums.ActionType Type => OpenCombatEngine.Core.Enums.ActionType.Action;
            public Result<ActionResult> Execute(OpenCombatEngine.Core.Interfaces.Actions.IActionContext context)
            {
                return Result<ActionResult>.Success(new ActionResult(true, "Critical Hit! 20 damage!"));
            }
        }

        private class FakeNormalAction : OpenCombatEngine.Core.Interfaces.Actions.IAction
        {
            public string Name => "Fake Hit";
            public string Description => "Normal hit";
            public OpenCombatEngine.Core.Enums.ActionType Type => OpenCombatEngine.Core.Enums.ActionType.Action;
            public Result<ActionResult> Execute(OpenCombatEngine.Core.Interfaces.Actions.IActionContext context)
            {
                return Result<ActionResult>.Success(new ActionResult(true, "Hit! 5 damage."));
            }
        }
    }
}
