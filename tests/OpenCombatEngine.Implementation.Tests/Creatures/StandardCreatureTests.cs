using FluentAssertions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class StandardCreatureTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties()
        {
            // Arrange
            var scores = new StandardAbilityScores();
            var hp = new StandardHitPoints(10);
            var id = Guid.NewGuid();

            // Act
            var creature = new StandardCreature("Goblin", scores, hp, null, id);

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
            var hp = new StandardHitPoints(10);

            // Act
            var creature = new StandardCreature("Goblin", scores, hp);

            // Assert
            creature.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_Name()
        {
            // Act
            Action act = () => new StandardCreature(null!, new StandardAbilityScores(), new StandardHitPoints(10));

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*name cannot be null*");
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_Components()
        {
            // Act
            Action act1 = () => new StandardCreature("Goblin", null!, new StandardHitPoints(10));
            Action act2 = () => new StandardCreature("Goblin", new StandardAbilityScores(), null!);

            // Assert
            act1.Should().Throw<ArgumentNullException>();
            act2.Should().Throw<ArgumentNullException>();
        }
    }
}
