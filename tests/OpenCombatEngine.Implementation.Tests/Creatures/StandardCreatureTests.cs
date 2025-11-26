using FluentAssertions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
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
            var creature = new StandardCreature(id.ToString(), "Goblin", scores, hp, new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

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
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Goblin", scores, hp, new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            // Assert
            creature.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_Name()
        {
            // Act
            Action act = () => new StandardCreature(Guid.NewGuid().ToString(), null!, new StandardAbilityScores(), new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Name cannot be empty*");
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_AbilityScores()
        {
            // Act
            Action act = () => new StandardCreature(Guid.NewGuid().ToString(), "Goblin", null!, new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
