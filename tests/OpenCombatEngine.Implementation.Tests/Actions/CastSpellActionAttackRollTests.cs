using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    /// <summary>
    /// Tests for the attack-roll gating <see cref="CastSpellAction"/>
    /// gained so <see cref="ISpell.RequiresAttackRoll"/> is no longer moot
    /// — before this, damage from an attack-roll spell (Ray of Frost,
    /// Scorching Ray, Guiding Bolt, ...) always applied unconditionally,
    /// same as an auto-hit spell like Magic Missile, since nothing ever
    /// rolled the attack at all.
    /// </summary>
    public class CastSpellActionAttackRollTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _caster;
        private readonly ICreature _target;
        private readonly ISpellCaster _spellcasting;

        public CastSpellActionAttackRollTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            _caster = Substitute.For<ICreature>();
            _target = Substitute.For<ICreature>();
            _spellcasting = Substitute.For<ISpellCaster>();

            _caster.Spellcasting.Returns(_spellcasting);
            _caster.ActionEconomy.HasAction.Returns(true);

            var combatStats = Substitute.For<ICombatStats>();
            combatStats.ArmorClass.Returns(15);
            _target.CombatStats.Returns(combatStats);
        }

        private CastSpellAction PrepareAndCast(ISpell spell, out Result<ActionResult> result)
        {
            _spellcasting.PreparedSpells.Returns(new List<ISpell> { spell });
            _spellcasting.HasSlot(spell.Level).Returns(true);
            _spellcasting.ConsumeSlot(spell.Level).Returns(Result<bool>.Success(true));

            var action = new CastSpellAction(spell, diceRoller: _diceRoller);
            var context = new StandardActionContext(_caster, new CreatureTarget(_target));
            result = action.Execute(context);
            return action;
        }

        [Fact]
        public void Execute_AttackRollHits_DealsDamage()
        {
            var spell = new Spell(
                "Ray of Frost", 0, SpellSchool.Evocation, "1 Action", "60 feet", "V, S", "Instantaneous", "A beam of cold.",
                _diceRoller, requiresAttackRoll: true,
                damageRolls: new List<DamageFormula> { new DamageFormula("1d8", DamageType.Cold) });

            // Target AC is 15; a natural 16 with +0 attack bonus (the
            // mocked ISpellCaster isn't a StandardSpellCaster, so this
            // test's attack bonus is always 0 — see RollSpellAttack's own
            // doc comment) clears it.
            _diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(16, "1d20", new List<int> { 16 }, 0, RollType.Normal)));
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            PrepareAndCast(spell, out var result);

            result.IsSuccess.Should().BeTrue();
            result.Value.Message.Should().Contain("hits AC 15");
            result.Value.Message.Should().Contain("Dealt 6 damage");
            _target.HitPoints.Received(1).TakeDamage(6, DamageType.Cold);
        }

        [Fact]
        public void Execute_AttackRollMisses_DealsNoDamage()
        {
            var spell = new Spell(
                "Ray of Frost", 0, SpellSchool.Evocation, "1 Action", "60 feet", "V, S", "Instantaneous", "A beam of cold.",
                _diceRoller, requiresAttackRoll: true,
                damageRolls: new List<DamageFormula> { new DamageFormula("1d8", DamageType.Cold) });

            // Natural 10 with +0 attack bonus does not clear AC 15.
            _diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10 }, 0, RollType.Normal)));
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            PrepareAndCast(spell, out var result);

            result.IsSuccess.Should().BeTrue();
            result.Value.Message.Should().Contain("misses AC 15");
            result.Value.Message.Should().Contain("Dealt 0 damage");
            _target.HitPoints.DidNotReceive().TakeDamage(Arg.Any<int>(), Arg.Any<DamageType>());
        }

        [Fact]
        public void Execute_AttackRollNatural20_AlwaysHitsEvenBelowTargetAC()
        {
            var spell = new Spell(
                "Ray of Frost", 0, SpellSchool.Evocation, "1 Action", "60 feet", "V, S", "Instantaneous", "A beam of cold.",
                _diceRoller, requiresAttackRoll: true,
                damageRolls: new List<DamageFormula> { new DamageFormula("1d8", DamageType.Cold) });

            // Target's own armor class is absurdly high (30); a natural 20
            // still always hits per SRD, regardless of the total.
            var highAcTarget = Substitute.For<ICreature>();
            var combatStats = Substitute.For<ICombatStats>();
            combatStats.ArmorClass.Returns(30);
            highAcTarget.CombatStats.Returns(combatStats);

            _diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(20, "1d20", new List<int> { 20 }, 0, RollType.Normal)));
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            _spellcasting.PreparedSpells.Returns(new List<ISpell> { spell });
            _spellcasting.HasSlot(spell.Level).Returns(true);
            _spellcasting.ConsumeSlot(spell.Level).Returns(Result<bool>.Success(true));

            var action = new CastSpellAction(spell, diceRoller: _diceRoller);
            var context = new StandardActionContext(_caster, new CreatureTarget(highAcTarget));
            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            result.Value.Message.Should().Contain("hits AC 30");
            highAcTarget.HitPoints.Received(1).TakeDamage(6, DamageType.Cold);
        }

        [Fact]
        public void Execute_AttackRollNatural1_AlwaysMissesEvenAboveTargetAC()
        {
            var spell = new Spell(
                "Ray of Frost", 0, SpellSchool.Evocation, "1 Action", "60 feet", "V, S", "Instantaneous", "A beam of cold.",
                _diceRoller, requiresAttackRoll: true,
                damageRolls: new List<DamageFormula> { new DamageFormula("1d8", DamageType.Cold) });

            // Target's armor class is trivially low (1); a natural 1
            // still always misses per SRD, regardless of the total.
            var lowAcTarget = Substitute.For<ICreature>();
            var combatStats = Substitute.For<ICombatStats>();
            combatStats.ArmorClass.Returns(1);
            lowAcTarget.CombatStats.Returns(combatStats);

            _diceRoller.Roll("1d20").Returns(Result<DiceRollResult>.Success(new DiceRollResult(1, "1d20", new List<int> { 1 }, 0, RollType.Normal)));
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            _spellcasting.PreparedSpells.Returns(new List<ISpell> { spell });
            _spellcasting.HasSlot(spell.Level).Returns(true);
            _spellcasting.ConsumeSlot(spell.Level).Returns(Result<bool>.Success(true));

            var action = new CastSpellAction(spell, diceRoller: _diceRoller);
            var context = new StandardActionContext(_caster, new CreatureTarget(lowAcTarget));
            var result = action.Execute(context);

            result.IsSuccess.Should().BeTrue();
            result.Value.Message.Should().Contain("misses AC 1");
            lowAcTarget.HitPoints.DidNotReceive().TakeDamage(Arg.Any<int>(), Arg.Any<DamageType>());
        }

        [Fact]
        public void Execute_SaveBasedSpell_NeverRollsAttackEvenIfRequiresAttackRollIsSet()
        {
            // SRD spells use an attack roll or a save, never both — this
            // defends the "never both" half of that invariant. A save
            // roll comes back Result.Failure so no accidental
            // saveSuccess=true/false confusion masks the assertion.
            var spell = new Spell(
                "Ambiguous", 1, SpellSchool.Evocation, "1 Action", "60 feet", "V, S", "Instantaneous", "Should never happen in real SRD data.",
                _diceRoller, requiresAttackRoll: true, saveAbility: Ability.Dexterity, saveEffect: SaveEffect.Negate,
                damageRolls: new List<DamageFormula> { new DamageFormula("1d8", DamageType.Cold) });

            _target.Checks.RollSavingThrow(Ability.Dexterity).Returns(Result<DiceRollResult>.Failure("no roll"));
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            PrepareAndCast(spell, out var result);

            result.IsSuccess.Should().BeTrue();
            _diceRoller.DidNotReceive().Roll("1d20");
        }
    }
}
