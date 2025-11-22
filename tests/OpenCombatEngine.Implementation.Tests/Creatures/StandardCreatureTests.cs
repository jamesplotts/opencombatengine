using FluentAssertions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;
using System;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class StandardCreatureTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties()
        {
            // Arrange
            var scores = new StandardAbilityScores();
            var hp = new StandardHitPoints(10, 10, 0);
            var id = Guid.NewGuid();

            // Act
            var creature = new StandardCreature(id.ToString(), "Goblin", scores, hp);

            // Assert
            creature.Name.Should().Be("Goblin");
            creature.AbilityScores.Should().BeSameAs(scores);
            creature.HitPoints.Should().BeSameAs(hp);
            creature.Id.Should().Be(id);
        }

        [Fact]
        public void Constructor_Should_Generate_Id_If_Null()
        {
            // Arrange
            var scores = new StandardAbilityScores();
            var hp = new StandardHitPoints(10, 10, 0);

            // Act
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Goblin", scores, hp);

            // Assert
            creature.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_Name()
        {
            // Act
            Action act = () => new StandardCreature(Guid.NewGuid().ToString(), null!, new StandardAbilityScores(), new StandardHitPoints(10, 10, 0));

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Name cannot be empty*");
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_AbilityScores()
        {
            // Act
            Action act = () => new StandardCreature(Guid.NewGuid().ToString(), "Goblin", null!, new StandardHitPoints(10, 10, 0));

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
