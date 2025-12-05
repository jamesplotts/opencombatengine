using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Implementation.Features;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ProficiencyFeatureTests
    {
        [Fact]
        public void OnApplied_Should_Add_Skill_Proficiency()
        {
            // Arrange
            var feature = new ProficiencyFeature("Stealthy", "Stealth");
            var creature = Substitute.For<ICreature>();
            var checks = Substitute.For<ICheckManager>();
            creature.Checks.Returns(checks);

            // Act
            feature.OnApplied(creature);

            // Assert
            checks.Received(1).AddSkillProficiency("Stealth");
        }

        [Fact]
        public void OnRemoved_Should_Remove_Skill_Proficiency()
        {
            // Arrange
            var feature = new ProficiencyFeature("Stealthy", "Stealth");
            var creature = Substitute.For<ICreature>();
            var checks = Substitute.For<ICheckManager>();
            creature.Checks.Returns(checks);

            // Act
            feature.OnRemoved(creature);

            // Assert
            checks.Received(1).RemoveSkillProficiency("Stealth");
        }

        [Fact]
        public void OnApplied_Should_Add_SavingThrow_Proficiency()
        {
            // Arrange
            var feature = new ProficiencyFeature("Resilient", Ability.Constitution);
            var creature = Substitute.For<ICreature>();
            var checks = Substitute.For<ICheckManager>();
            creature.Checks.Returns(checks);

            // Act
            feature.OnApplied(creature);

            // Assert
            checks.Received(1).AddSavingThrowProficiency(Ability.Constitution);
        }
    }
}
