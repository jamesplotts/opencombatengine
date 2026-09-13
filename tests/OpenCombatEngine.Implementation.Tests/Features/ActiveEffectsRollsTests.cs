using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Effects;
using OpenCombatEngine.Implementation.Items;
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

            // A deterministic dice roller: every ability check in this
            // class rolls with a +0 modifier (no ability score bonus, no
            // proficiency granted anywhere below), so "1d20+0" is the only
            // notation RollAbilityCheck ever builds here. This lets the
            // ability-check tests below assert an exact Total instead of
            // only checking that a call didn't throw.
            var diceRoller = Substitute.For<IDiceRoller>();
            diceRoller.Roll("1d20+0").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(10, "1d20+0", new System.Collections.Generic.List<int> { 10 }, 0, RollType.Normal)
            ));

            _creature = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Test Creature",
                _abilityScores,
                Substitute.For<IHitPoints>(),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller()),
                equipmentManager: equipment,
                defaultDiceRoller: diceRoller
            );
            _creature.Team = "Neutral";
            
            // Ensure we have a spellcaster
            var caster = new StandardSpellCaster(
                Ability.Intelligence,
                a => _creature.AbilityScores.GetModifier(a),
                () => _creature.ProficiencyBonus
            );
            _creature.SetSpellCaster(caster);
            _spellCaster = _creature.Spellcasting!;

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
            // The class-level dice roller is a substitute fixed on
            // "1d20+0" (see the constructor above), so the raw roll is
            // deterministically 10 — this whole-ability-check effect
            // applies regardless of which ability or skill was rolled.
            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(11); // 10 raw + 1 Guidance
        }

        [Fact]
        public void Should_Apply_SkillScoped_Bonus_Only_To_The_Matching_Skill()
        {
            // The skill-scoped sibling of Should_Apply_Ability_Check_Bonus
            // above — a SkillBonusFeature-style effect (e.g. a "+2
            // Intimidation" item) must not leak onto a different named
            // skill, or a bare ability check with no named skill at all.
            var effect = new StatBonusEffect("Ring of Intimidation", "+2 Intimidation", -1, StatType.AbilityCheck, 2, targetSkillName: "Intimidation");
            _effectManager.AddEffect(effect);

            var intimidation = _creature.Checks.RollAbilityCheck(Ability.Charisma, "Intimidation");
            var persuasion = _creature.Checks.RollAbilityCheck(Ability.Charisma, "Persuasion");
            var bareCheck = _creature.Checks.RollAbilityCheck(Ability.Charisma);

            intimidation.Value.Total.Should().Be(12); // 10 raw + 2
            persuasion.Value.Total.Should().Be(10); // different named skill: no bonus
            bareCheck.Value.Total.Should().Be(10); // no named skill at all: no bonus
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
