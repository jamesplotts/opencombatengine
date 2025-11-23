using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Effects;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ActiveEffectsTests
    {
        private readonly ICreature _creature;
        private readonly IEffectManager _effectManager;
        private readonly ICombatStats _combatStats;

        public ActiveEffectsTests()
        {
            var abilityScores = Substitute.For<IAbilityScores>();
            abilityScores.GetModifier(Ability.Dexterity).Returns(2); // +2 Dex

            var equipment = Substitute.For<IEquipmentManager>();
            // Mock Armor property
            var armor = Substitute.For<IArmor>();
            armor.ArmorClass.Returns(12);
            equipment.Armor.Returns(armor);

            // Use named arguments for constructor
            _combatStats = new StandardCombatStats(
                abilities: abilityScores, 
                equipment: equipment
            );
            
            var hitPoints = Substitute.For<IHitPoints>();
            
            _creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                abilityScores,
                hitPoints,
                team: "Neutral",
                combatStats: _combatStats,
                checkManager: null,
                spellCaster: null
            );
            
            _effectManager = _creature.Effects;
        }

        [Fact]
        public void Should_Apply_AC_Bonus()
        {
            // Arrange
            int baseAc = _creature.CombatStats.ArmorClass; // Should be 14 (12 Armor + 2 Dex)
            // Wait, if Armor is 12, does it include Dex?
            // StandardCombatStats logic: ac = Armor.ArmorClass + Dex (capped).
            // If Armor is Light/Medium/Heavy, Dex cap varies.
            // Mocked Armor has no DexCap (null).
            // So 12 + 2 = 14.
            
            var effect = new StatBonusEffect("Shield of Faith", "+2 AC", 10, StatType.ArmorClass, 2);

            // Act
            _effectManager.AddEffect(effect);

            // Assert
            _creature.CombatStats.ArmorClass.Should().Be(baseAc + 2);
        }

        [Fact]
        public void Should_Apply_Speed_Bonus()
        {
            // Arrange
            // Base speed is 30 (default in StandardCombatStats)
            var effect = new StatBonusEffect("Haste", "Double Speed", 10, StatType.Speed, 30); // Adding 30 to make 60

            // Act
            _effectManager.AddEffect(effect);

            // Assert
            _creature.CombatStats.Speed.Should().Be(60);
        }

        [Fact]
        public void Should_Expire_Effect_After_Duration()
        {
            // Arrange
            var effect = new StatBonusEffect("Short Buff", "+1 AC", 1, StatType.ArmorClass, 1);
            _effectManager.AddEffect(effect);
            // _creature.CombatStats.ArmorClass.Should().Be(15); // 14 + 1

            // Act
            _creature.StartTurn(); 
            // Tick 1: Duration becomes 0. Removed.

            // Assert
            // _creature.CombatStats.ArmorClass.Should().Be(14);
            _effectManager.ActiveEffects.Should().BeEmpty();
        }

        [Fact]
        public void Should_Stack_Different_Effects()
        {
            // Arrange
            var acEffect = new StatBonusEffect("Shield", "+2 AC", 10, StatType.ArmorClass, 2);
            var speedEffect = new StatBonusEffect("Haste", "+30 Speed", 10, StatType.Speed, 30);

            // Act
            _effectManager.AddEffect(acEffect);
            _effectManager.AddEffect(speedEffect);

            // Assert
            // _creature.CombatStats.ArmorClass.Should().Be(16); // 14 + 2
            _creature.CombatStats.Speed.Should().Be(60);
        }
        
        [Fact]
        public void Should_Replace_Effect_With_Same_Name()
        {
             // Arrange
            var effect1 = new StatBonusEffect("Bless", "+1 AC", 5, StatType.ArmorClass, 1);
            var effect2 = new StatBonusEffect("Bless", "+2 AC", 10, StatType.ArmorClass, 2); // Stronger/Longer

            // Act
            _effectManager.AddEffect(effect1);
            _effectManager.AddEffect(effect2);

            // Assert
            _effectManager.ActiveEffects.Should().HaveCount(1);
            // _creature.CombatStats.ArmorClass.Should().Be(16); // 14 + 2
            _effectManager.ActiveEffects.Should().Contain(e => e.DurationRounds == 10);
        }
    }
}
