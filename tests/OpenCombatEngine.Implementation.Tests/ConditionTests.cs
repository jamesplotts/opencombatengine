using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class ConditionTests
    {
        [Fact]
        public void ConditionManager_Should_Add_And_Remove_Conditions()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var manager = new StandardConditionManager(creature);
            var condition = new Condition("Blinded", "Cannot see", 3);

            // Act
            manager.AddCondition(condition);

            // Assert
            manager.ActiveConditions.Should().Contain(condition);

            // Act
            manager.RemoveCondition("Blinded");

            // Assert
            manager.ActiveConditions.Should().BeEmpty();
        }

        [Fact]
        public void ConditionManager_Should_Decrement_Duration_On_Tick()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var manager = new StandardConditionManager(creature);
            var condition = new Condition("Stunned", "Cannot act", 2);
            
            manager.AddCondition(condition);

            // Act - Tick 1 (Duration 2 -> 1)
            manager.Tick();
            manager.ActiveConditions.Should().Contain(condition);
            condition.DurationRounds.Should().Be(1);

            // Act - Tick 2 (Duration 1 -> 0)
            manager.Tick();
            // Should still be there? Or removed immediately?
            // Logic: Tick calls OnTurnStart -> Decrement. If 0, remove.
            // So after this tick, duration is 0, and it should be removed.
            manager.ActiveConditions.Should().BeEmpty();
        }

        [Fact]
        public void Condition_Should_Trigger_OnApplied_And_OnRemoved()
        {
            // Arrange
            var creature = Substitute.For<ICreature>();
            var manager = new StandardConditionManager(creature);
            var condition = Substitute.For<Condition>("Prone", "Lying down", 1, ConditionType.Prone, null);

            // Act
            manager.AddCondition(condition);

            // Assert
            condition.Received(1).OnApplied(creature);

            // Act
            manager.RemoveCondition("Prone");

            // Assert
            condition.Received(1).OnRemoved(creature);
        }
    }
}
