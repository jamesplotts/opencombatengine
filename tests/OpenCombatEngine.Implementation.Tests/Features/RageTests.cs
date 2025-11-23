using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Creatures;
using System.Collections.Generic;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class RageTests
    {
        [Fact]
        public void RageAction_Should_Apply_RageCondition()
        {
            // Arrange
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Barbarian",
                new StandardAbilityScores(),
                new StandardHitPoints(20),
                "Neutral",
                new StandardCombatStats()
            );
            var action = new RageAction();

            // Act
            // Act
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                creature, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(creature) // Self target
            );
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            creature.Conditions.HasCondition(ConditionType.Custom).Should().BeTrue(); // Rage is Custom type
            creature.CombatStats.Resistances.Should().Contain(DamageType.Bludgeoning);
            creature.CombatStats.Resistances.Should().Contain(DamageType.Piercing);
            creature.CombatStats.Resistances.Should().Contain(DamageType.Slashing);
        }

        [Fact]
        public void RageCondition_Should_Halve_Damage()
        {
            // Arrange
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Barbarian",
                new StandardAbilityScores(),
                new StandardHitPoints(20),
                "Neutral",
                new StandardCombatStats()
            );
            
            // Apply Rage manually
            var rage = new RageCondition();
            creature.Conditions.AddCondition(rage);

            var attack = new AttackResult(
                null, 
                creature, 
                20, // Hit
                false, 
                false, 
                false, 
                new List<DamageRoll> { new DamageRoll(10, DamageType.Slashing) }
            );

            // Act
            var outcome = creature.ResolveAttack(attack);

            // Assert
            outcome.DamageDealt.Should().Be(5); // 10 / 2 = 5
            creature.HitPoints.Current.Should().Be(15); // 20 - 5
        }

        [Fact]
        public void RageCondition_Should_Not_Halve_Magical_Damage()
        {
            // Arrange
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Barbarian",
                new StandardAbilityScores(),
                new StandardHitPoints(20),
                "Neutral",
                new StandardCombatStats()
            );
            
            var rage = new RageCondition();
            creature.Conditions.AddCondition(rage);

            var attack = new AttackResult(
                null, 
                creature, 
                20, 
                false, 
                false, 
                false, 
                new List<DamageRoll> { new DamageRoll(10, DamageType.Fire) }
            );

            // Act
            var outcome = creature.ResolveAttack(attack);

            // Assert
            outcome.DamageDealt.Should().Be(10); // Not resistant to Fire
            creature.HitPoints.Current.Should().Be(10);
        }
    }
}
