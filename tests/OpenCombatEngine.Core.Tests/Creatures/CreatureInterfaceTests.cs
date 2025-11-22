using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Results;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public int Current { get; set; } = 10;
        public int Max { get; set; } = 20;
        public int Temporary { get; set; } = 5;
        public bool IsDead => Current <= 0;
        public event EventHandler<DamageTakenEventArgs>? DamageTaken;
        public event EventHandler<HealedEventArgs>? Healed;
        public event EventHandler<DeathEventArgs>? Died;
        public void TakeDamage(int amount) => DamageTaken?.Invoke(this, new DamageTakenEventArgs(amount, 10));
        public void TakeDamage(int amount, OpenCombatEngine.Core.Enums.DamageType type) => TakeDamage(amount);
        public void Heal(int amount) => Healed?.Invoke(this, new HealedEventArgs(amount, 10));
        public int DeathSaveSuccesses { get; set; }
        public int DeathSaveFailures { get; set; }
        public bool IsStable { get; set; }
        public void RecordDeathSave(bool success, bool critical = false) { }
        public void Stabilize() { IsStable = true; }
    }

    private class StubCreature : ICreature
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Test Creature";
        public IAbilityScores AbilityScores { get; set; } = new StubAbilityScores();
        public IHitPoints HitPoints { get; set; } = new StubHitPoints();
        public ICombatStats CombatStats { get; set; } = new StubCombatStats();
        public IConditionManager Conditions { get; set; } = new StubConditionManager();
        public void StartTurn() { }
        public void EndTurn() { }
        public IActionEconomy ActionEconomy { get; set; } = new StubActionEconomy();
        public IMovement Movement { get; set; } = new StubMovement();
        public ICheckManager Checks { get; set; } = new StubCheckManager();
        public int ProficiencyBonus => 2;
    }

    private class StubCheckManager : ICheckManager
    {
        public Result<int> RollAbilityCheck(Ability ability) => Result<int>.Success(10);
        public Result<int> RollSavingThrow(Ability ability) => Result<int>.Success(10);
        public Result<int> RollDeathSave() => Result<int>.Success(10);
    }

    private class StubMovement : IMovement
    {
        public int Speed => 30;
        public int MovementRemaining => 30;
        public void Move(int distance) { }
        public void ResetTurn() { }
    }

    private class StubActionEconomy : IActionEconomy
    {
        public bool HasAction => true;
        public bool HasBonusAction => true;
        public bool HasReaction => true;
        public void UseAction() { }
        public void UseBonusAction() { }
        public void UseReaction() { }
        public void ResetTurn() { }
        public void ResetReaction() { }
    }

    private class StubConditionManager : IConditionManager
    {
        public IEnumerable<ICondition> ActiveConditions => Enumerable.Empty<ICondition>();
        public bool HasCondition(ConditionType type) => false;
        public Result<bool> AddCondition(ICondition condition) => Result<bool>.Success(true);
        public void RemoveCondition(string conditionName) { }
        public void Tick() { }
    }

    private class StubCombatStats : ICombatStats
    {
        public int ArmorClass => 10;
        public int InitiativeBonus => 0;
        public int Speed => 30;
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Resistances { get; } = new HashSet<OpenCombatEngine.Core.Enums.DamageType>();
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Vulnerabilities { get; } = new HashSet<OpenCombatEngine.Core.Enums.DamageType>();
        public IReadOnlySet<OpenCombatEngine.Core.Enums.DamageType> Immunities { get; } = new HashSet<OpenCombatEngine.Core.Enums.DamageType>();
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
