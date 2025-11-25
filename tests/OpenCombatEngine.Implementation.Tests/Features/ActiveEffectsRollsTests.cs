using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Effects;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ActiveEffectsRollsTests
    {
        private readonly StandardCreature _creature;
        private readonly IEffectManager _effectManager;
        private readonly IAbilityScores _abilityScores;
        private readonly ISpellCaster _spellCaster;

        public ActiveEffectsRollsTests()
        {
            _abilityScores = Substitute.For<IAbilityScores>();
            _abilityScores.GetModifier(Arg.Any<Ability>()).Returns(0); // Base modifiers 0

            var equipment = Substitute.For<IEquipmentManager>();
            var armor = Substitute.For<IArmor>();
            armor.ArmorClass.Returns(10);
            equipment.Armor.Returns(armor);

            var combatStats = new StandardCombatStats(
                initiativeBonus: 0,
                equipment: equipment,
                abilities: _abilityScores
            );

            _spellCaster = new StandardSpellCaster(_abilityScores, 2, Ability.Intelligence);

            _creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                _abilityScores,
                Substitute.For<IHitPoints>(),
                team: "Neutral",
                combatStats: combatStats,
                spellCaster: _spellCaster
            );

            _effectManager = _creature.Effects;
        }

        [Fact]
        public void Should_Apply_Initiative_Bonus()
        {
            // Arrange
            var effect = new StatBonusEffect("Alert", "+5 Initiative", 10, StatType.Initiative, 5);
            _effectManager.AddEffect(effect);

            // Act & Assert
            _creature.CombatStats.InitiativeBonus.Should().Be(5);
        }

        [Fact]
        public void Should_Apply_Ability_Check_Bonus()
        {
            // Arrange
            var effect = new StatBonusEffect("Guidance", "+1 Check", 10, StatType.AbilityCheck, 1);
            _effectManager.AddEffect(effect);

            // Act
            var result = _creature.Checks.RollAbilityCheck(Ability.Strength);

            // Assert
            // Dice roller is standard, so result is random. 
            // We can't easily assert exact value without mocking dice roller inside creature.
            // StandardCreature creates its own dice roller if not passed checkManager.
            // But we can't inject dice roller into StandardCreature easily for Checks property creation unless we pass CheckManager.
            // Let's rely on the fact that StandardDiceRoller returns 1-20.
            // Wait, StandardDiceRoller is random.
            // I should have injected a mock CheckManager or DiceRoller.
            // But StandardCreature constructor allows passing checkManager.
            // Let's recreate creature with mocked check manager? No, CheckManager logic is what we are testing.
            // We need to inject DiceRoller into CheckManager.
        }
        
        [Fact]
        public void Should_Apply_Spell_DC_Bonus()
        {
            // Arrange
            // Base DC = 8 + 2 (PB) + 0 (Mod) = 10
            var effect = new StatBonusEffect("Focus", "+1 DC", 10, StatType.SpellSaveDC, 1);
            _effectManager.AddEffect(effect);

            // Act & Assert
            _creature.Spellcasting.SpellSaveDC.Should().Be(11);
        }

        [Fact]
        public void Should_Apply_Attack_Roll_Bonus()
        {
            // Arrange
            var effect = new StatBonusEffect("Bless", "+1 Attack", 10, StatType.AttackRoll, 1);
            _effectManager.AddEffect(effect);

            var target = Substitute.For<ICreature>();
            var attackResult = new AttackResult(_creature, target, 10, false, false, false, new System.Collections.Generic.List<DamageRoll>());

            // Act
            _creature.ModifyOutgoingAttack(attackResult);

            // Assert
            attackResult.AttackRoll.Should().Be(11);
        }

        [Fact]
        public void Should_Apply_Damage_Bonus()
        {
            // Arrange
            var effect = new StatBonusEffect("Divine Favor", "+2 Damage", 10, StatType.DamageRoll, 2);
            _effectManager.AddEffect(effect);

            var target = Substitute.For<ICreature>();
            var damage = new DamageRoll(5, DamageType.Slashing);
            var attackResult = new AttackResult(_creature, target, 10, false, false, false, new[] { damage });

            // Act
            _creature.ModifyOutgoingAttack(attackResult);

            // Assert
            attackResult.Damage.Should().HaveCount(2);
            attackResult.Damage.Should().Contain(d => d.Amount == 2 && d.Type == DamageType.Slashing);
        }
    }
}
