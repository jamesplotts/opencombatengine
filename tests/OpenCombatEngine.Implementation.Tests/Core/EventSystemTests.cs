using System;
using System.Collections.Generic;
using FluentAssertions;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spatial;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Enums;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Core
{
    public class EventSystemTests
    {
        [Fact]
        public void Movement_Event_Should_Fire_On_Grid_Move()
        {
            // Arrange
            var creature = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Runner",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var grid = new StandardGridManager();
            grid.PlaceCreature(creature, new Position(0, 0, 0));

            bool eventFired = false;
            Position from = new Position(-1, -1, -1);
            Position to = new Position(-1, -1, -1);

            creature.Movement.Moved += (sender, args) =>
            {
                eventFired = true;
                from = args.From;
                to = args.To;
            };

            // Act
            grid.MoveCreature(creature, new Position(1, 0, 0));

            // Assert
            eventFired.Should().BeTrue();
            from.Should().Be(new Position(0, 0, 0));
            to.Should().Be(new Position(1, 0, 0));
        }

        [Fact]
        public void Condition_Events_Should_Fire()
        {
            // Arrange
            var creature = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Victim",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            bool addedFired = false;
            bool removedFired = false;

            creature.Conditions.ConditionAdded += (sender, args) => addedFired = true;
            creature.Conditions.ConditionRemoved += (sender, args) => removedFired = true;

            var condition = ConditionFactory.Create(ConditionType.Blinded);

            // Act - Add
            creature.Conditions.AddCondition(condition);

            // Assert - Add
            addedFired.Should().BeTrue();

            // Act - Remove
            creature.Conditions.RemoveCondition(condition.Name);

            // Assert - Remove
            removedFired.Should().BeTrue();
        }

        [Fact]
        public void Action_Events_Should_Fire()
        {
            // Arrange
            var creature = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Actor",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            bool startedFired = false;
            bool endedFired = false;
            string actionName = "";

            creature.ActionStarted += (sender, args) => 
            {
                startedFired = true;
                actionName = args.ActionName;
            };

            creature.ActionEnded += (sender, args) => 
            {
                endedFired = true;
                args.ActionName.Should().Be(actionName);
            };

            var action = new MoveAction(30);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(creature, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(creature)); 
            // Note: MoveAction might fail without Grid or Movement, but PerformAction should still fire start/end events regardless of action success/failure usually?
            // Wait, StandardCreature.PerformAction calls action.Execute. If action.Execute throws, ActionEnded might effectively be skipped or throw.
            // MoveAction usually returns Result, not throws. So it should work.

            // Act
            creature.PerformAction(action, context);

            // Assert
            startedFired.Should().BeTrue();
            endedFired.Should().BeTrue();
            actionName.Should().Be("Move");
        }
    }
}
