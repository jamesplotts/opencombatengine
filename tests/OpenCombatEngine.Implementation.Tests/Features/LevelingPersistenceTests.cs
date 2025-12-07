using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Implementation.Classes;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class LevelingPersistenceTests
    {
        [Fact]
        public void Should_Restore_Experience_And_Levels()
        {
            // Arrange
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Hero",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var fighter = new ClassDefinition("Fighter", 10);
            creature.LevelManager.LevelUp(fighter);
            creature.LevelManager.AddExperience(500);

            // Act
            var state = creature.GetState();
            var restoredCreature = new StandardCreature(state);

            // Assert
            restoredCreature.LevelManager.ExperiencePoints.Should().Be(500);
            restoredCreature.LevelManager.TotalLevel.Should().Be(1);
            restoredCreature.LevelManager.Classes.Keys.First().Name.Should().Be("Fighter");
            restoredCreature.LevelManager.Classes.Keys.First().HitDie.Should().Be(10);
            restoredCreature.LevelManager.Classes.Values.First().Should().Be(1);
        }

        [Fact]
        public void Should_Handle_Multiclassing_Persistence()
        {
            // Arrange
            var creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Multiclass Hero",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );

            var fighter = new ClassDefinition("Fighter", 10);
            var rogue = new ClassDefinition("Rogue", 8);

            creature.LevelManager.LevelUp(fighter);
            creature.LevelManager.LevelUp(rogue);

            // Act
            var state = creature.GetState();
            var restoredCreature = new StandardCreature(state);

            // Assert
            restoredCreature.LevelManager.TotalLevel.Should().Be(2);
            restoredCreature.LevelManager.Classes.Count.Should().Be(2);
            
            var restoredFighter = restoredCreature.LevelManager.Classes.Keys.First(c => c.Name == "Fighter");
            restoredCreature.LevelManager.Classes[restoredFighter].Should().Be(1);
            
            var restoredRogue = restoredCreature.LevelManager.Classes.Keys.First(c => c.Name == "Rogue");
            restoredCreature.LevelManager.Classes[restoredRogue].Should().Be(1);
        }
    }
}
