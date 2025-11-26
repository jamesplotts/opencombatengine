using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class EncumbranceTests
    {
        [Fact]
        public void Should_Calculate_Total_Weight_Recursively()
        {
            // Arrange
            var inventory = new StandardInventory();
            var backpack = new ContainerItem("Backpack", 5, 30, 1.0);
            var item1 = new Item("Rope", "Heavy rope", 10, 1);
            
            // Act
            backpack.AddItem(item1); // Backpack = 15
            inventory.AddItem(backpack);
            inventory.AddItem(new Item("Dagger", "Sharp", 1, 1)); // +1

            // Assert
            inventory.TotalWeight.Should().Be(16);
        }

        [Fact]
        public void Should_Determine_Encumbrance_Level()
        {
            // Arrange
            var abilityScores = new StandardAbilityScores(strength: 10);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Test", abilityScores, new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            
            // Act & Assert - None (Weight 50)
            creature.Inventory.AddItem(new Item("Load", "", 50));
            creature.EncumbranceLevel.Should().Be(EncumbranceLevel.None);

            // Act & Assert - Encumbered (Weight 51)
            creature.Inventory.AddItem(new Item("Small Stone", "", 1));
            creature.EncumbranceLevel.Should().Be(EncumbranceLevel.Encumbered);

            // Act & Assert - Heavily Encumbered (Weight 101)
            creature.Inventory.AddItem(new Item("Big Stone", "", 50));
            creature.EncumbranceLevel.Should().Be(EncumbranceLevel.HeavilyEncumbered);

            // Act & Assert - Over Capacity (Weight 151)
            creature.Inventory.AddItem(new Item("Huge Stone", "", 50));
            creature.EncumbranceLevel.Should().Be(EncumbranceLevel.OverCapacity);
        }

        [Fact]
        public void Should_Apply_Speed_Penalties()
        {
            // Arrange
            var abilityScores = new StandardAbilityScores(strength: 10);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Test", abilityScores, new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            // Base speed is 30 by default in StandardCreature (or handled by Movement)
            // Wait, StandardCreature constructor initializes Movement with base speed 30?
            // Let's check StandardCreature constructor or Movement initialization.
            // Assuming base speed 30.
            
            // Act & Assert - None
            creature.Inventory.AddItem(new Item("Load", "", 50));
            creature.Movement.Speed.Should().Be(30);

            // Act & Assert - Encumbered (-10)
            creature.Inventory.AddItem(new Item("Pebble", "", 1)); // 51
            creature.Movement.Speed.Should().Be(20);

            // Act & Assert - Heavily Encumbered (-20)
            creature.Inventory.AddItem(new Item("Rock", "", 50)); // 101
            creature.Movement.Speed.Should().Be(10);

            // Act & Assert - Over Capacity (Speed 5)
            creature.Inventory.AddItem(new Item("Boulder", "", 50)); // 151
            creature.Movement.Speed.Should().Be(5);
        }
    }
}
