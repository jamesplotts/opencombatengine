using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Spells;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spells;
using Xunit;
using System.Collections.Generic;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class SpellEffectsTests
    {
        private readonly StandardCreature _caster;
        private readonly StandardCreature _target;
        private readonly StandardSpellCaster _spellCaster;

        public SpellEffectsTests()
        {
            var abilityScores = new StandardAbilityScores(intelligence: 18, constitution: 14); // Int +4, Con +2
            
            var mockCreature = Substitute.For<ICreature>();
            mockCreature.AbilityScores.Returns(abilityScores);
            mockCreature.ProficiencyBonus.Returns(2);
            
            _spellCaster = new StandardSpellCaster(
                Ability.Intelligence, 
                a => mockCreature.AbilityScores.GetModifier(a), 
                () => mockCreature.ProficiencyBonus
            );
            _caster = new StandardCreature(System.Guid.NewGuid().ToString(), "Caster", abilityScores, new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: _spellCaster);
            
            _target = new StandardCreature(System.Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            
            // Give slots
            _spellCaster.SetSlots(1, 4);
            _spellCaster.RestoreAllSlots();
        }

        [Fact]
        public void Fireball_Should_Deal_Damage()
        {
            var damageRolls = new List<DamageFormula> { new DamageFormula("8d6", DamageType.Fire) };
            var spell = new Spell("Fireball", 3, SpellSchool.Evocation, "Action", "150 ft", "V,S,M", "Instantaneous", "Boom", 
                new OpenCombatEngine.Implementation.Dice.StandardDiceRoller(),
                damageRolls: damageRolls, 
                saveAbility: Ability.Dexterity, 
                saveEffect: SaveEffect.HalfDamage);
            
            _spellCaster.LearnSpell(spell);
            _spellCaster.PrepareSpell(spell);
            _spellCaster.SetSlots(3, 1);
            _spellCaster.RestoreAllSlots();

            var action = new CastSpellAction(spell, 3);
            var context = new StandardActionContext(_caster, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            _target.HitPoints.Current.Should().BeLessThan(20);
        }

        [Fact]
        public void Healing_Word_Should_Heal()
        {
            var spell = new Spell("Healing Word", 1, SpellSchool.Evocation, "Bonus Action", "60 ft", "V", "Instantaneous", "Heal", 
                new OpenCombatEngine.Implementation.Dice.StandardDiceRoller(),
                healingDice: "1d4");

            _spellCaster.LearnSpell(spell);
            _spellCaster.PrepareSpell(spell);
            
            // Damage target first
            _target.HitPoints.TakeDamage(10);
            int hpBefore = _target.HitPoints.Current;

            var action = new CastSpellAction(spell, 1);
            var context = new StandardActionContext(_caster, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target));

            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            _target.HitPoints.Current.Should().BeGreaterThan(hpBefore);
        }

        [Fact]
        public void Save_Should_Halve_Damage()
        {
            // Mock target check manager to force save success
            var mockCheckManager = Substitute.For<ICheckManager>();
            mockCheckManager.RollSavingThrow(Arg.Any<Ability>()).Returns(OpenCombatEngine.Core.Results.Result<int>.Success(20)); // High roll
            
            var target = new StandardCreature(System.Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), checkManager: mockCheckManager);

            var damageRolls = new List<DamageFormula> { new DamageFormula("10", DamageType.Fire) }; // Fixed damage for testing
            var spell = new Spell("TestFire", 1, SpellSchool.Evocation, "Action", "60 ft", "V", "Instant", "Burn",
                new OpenCombatEngine.Implementation.Dice.StandardDiceRoller(),
                damageRolls: damageRolls,
                saveAbility: Ability.Dexterity,
                saveEffect: SaveEffect.HalfDamage);

            _spellCaster.LearnSpell(spell);
            _spellCaster.PrepareSpell(spell);
            
            var action = new CastSpellAction(spell, 1);
            var context = new StandardActionContext(_caster, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(target));

            action.Execute(context);

            // 10 damage halved = 5.
            target.HitPoints.Current.Should().Be(15);
        }

        [Fact]
        public void Save_Should_Negate_Damage_For_Cantrip()
        {
            var mockCheckManager = Substitute.For<ICheckManager>();
            mockCheckManager.RollSavingThrow(Arg.Any<Ability>()).Returns(OpenCombatEngine.Core.Results.Result<int>.Success(20)); 
            
            var target = new StandardCreature(System.Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), checkManager: mockCheckManager);

            var damageRolls = new List<DamageFormula> { new DamageFormula("10", DamageType.Radiant) };
            var spell = new Spell("Sacred Flame", 0, SpellSchool.Evocation, "Action", "60 ft", "V", "Instant", "Burn",
                new OpenCombatEngine.Implementation.Dice.StandardDiceRoller(),
                damageRolls: damageRolls,
                saveAbility: Ability.Dexterity,
                saveEffect: SaveEffect.Negate);

            _spellCaster.LearnSpell(spell);
            
            var action = new CastSpellAction(spell, 0);
            var context = new StandardActionContext(_caster, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(target));

            action.Execute(context);

            target.HitPoints.Current.Should().Be(20);
        }
    }
}
