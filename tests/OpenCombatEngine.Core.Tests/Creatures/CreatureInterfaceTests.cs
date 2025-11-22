using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using Xunit;
using System;

namespace OpenCombatEngine.Core.Tests.Creatures;

public class CreatureInterfaceTests
{
    // Stub implementations for testing composition
    private class StubAbilityScores : IAbilityScores
    {
        public int Strength => 10;
        public int Dexterity => 12;
        public int Constitution => 14;
        public int Intelligence => 16;
        public int Wisdom => 18;
        public int Charisma => 20;

        public int GetModifier(Ability ability) => (GetScore(ability) - 10) / 2;

        private int GetScore(Ability ability) => ability switch
        {
            Ability.Strength => Strength,
            Ability.Dexterity => Dexterity,
            Ability.Constitution => Constitution,
            Ability.Intelligence => Intelligence,
            Ability.Wisdom => Wisdom,
            Ability.Charisma => Charisma,
            _ => 0
        };
    }

    private class StubHitPoints : IHitPoints
    {
        public int Current => 10;
        public int Max => 20;
        public int Temporary => 5;
        public bool IsDead => Current <= 0;
    }

    private class StubCreature : ICreature
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; } = "Test Creature";
        public IAbilityScores AbilityScores { get; } = new StubAbilityScores();
        public IHitPoints HitPoints { get; } = new StubHitPoints();
    }

    [Fact]
    public void ICreature_Should_Compose_Components()
    {
        // Arrange
        var creature = new StubCreature();

        // Act & Assert
        creature.Id.Should().NotBeEmpty();
        creature.Name.Should().Be("Test Creature");
        
        creature.AbilityScores.Should().NotBeNull();
        creature.AbilityScores.Strength.Should().Be(10);
        
        creature.HitPoints.Should().NotBeNull();
        creature.HitPoints.Max.Should().Be(20);
    }

    [Theory]
    [InlineData(Ability.Strength, 0)]
    [InlineData(Ability.Dexterity, 1)]
    [InlineData(Ability.Constitution, 2)]
    [InlineData(Ability.Intelligence, 3)]
    [InlineData(Ability.Wisdom, 4)]
    [InlineData(Ability.Charisma, 5)]
    public void IAbilityScores_Should_Allow_Modifier_Calculation(Ability ability, int expectedMod)
    {
        // Arrange
        var scores = new StubAbilityScores();

        // Act
        var mod = scores.GetModifier(ability);

        // Assert
        mod.Should().Be(expectedMod);
    }
}
