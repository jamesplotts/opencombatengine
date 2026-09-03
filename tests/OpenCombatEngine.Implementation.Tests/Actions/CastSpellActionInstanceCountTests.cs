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
    /// Tests for <see cref="ISpell.InstanceCount"/>/
    /// <see cref="ISpell.InstanceCountPerUpcastLevel"/> — the fixed
    /// multi-instance scaling <see cref="CastSpellAction"/> gained so a
    /// spell like Magic Missile (three darts at 1st level, each
    /// independently rolled) or Scorching Ray (three rays, each its own
    /// attack roll) deals its real SRD damage instead of a single
    /// instance's worth. Before this, the only multiplier mechanism was
    /// cantrip level-scaling, which doesn't apply to a 1st-level+ spell at
    /// all.
    /// </summary>
    public class CastSpellActionInstanceCountTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _caster;
        private readonly ICreature _target;
        private readonly ISpellCaster _spellcasting;

        public CastSpellActionInstanceCountTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            _caster = Substitute.For<ICreature>();
            _target = Substitute.For<ICreature>();
            _spellcasting = Substitute.For<ISpellCaster>();

            _caster.Spellcasting.Returns(_spellcasting);
            _caster.ActionEconomy.HasAction.Returns(true);

            var combatStats = Substitute.For<ICombatStats>();
            combatStats.ArmorClass.Returns(10);
            _target.CombatStats.Returns(combatStats);
        }

        private Result<ActionResult> PrepareAndCast(ISpell spell, int? slotLevel = null)
        {
            _spellcasting.PreparedSpells.Returns(new List<ISpell> { spell });
            _spellcasting.HasSlot(Arg.Any<int>()).Returns(true);
            _spellcasting.ConsumeSlot(Arg.Any<int>()).Returns(Result<bool>.Success(true));

            var action = new CastSpellAction(spell, slotLevel, _diceRoller);
            var context = new StandardActionContext(_caster, new CreatureTarget(_target));
            return action.Execute(context);
        }

        [Fact]
        public void Execute_MagicMissileStyleSpell_RollsDamageOncePerInstance()
        {
            var spell = new Spell(
                "Magic Missile", 1, SpellSchool.Evocation, "1 Action", "120 feet", "V, S", "Instantaneous", "Three darts.",
                _diceRoller,
                damageRolls: new List<DamageFormula> { new DamageFormula("1d4+1", DamageType.Force) },
                instanceCount: 3);

            _diceRoller.Roll("1d4+1").Returns(Result<DiceRollResult>.Success(new DiceRollResult(3, "1d4+1", new List<int> { 2 }, 1, RollType.Normal)));

            var result = PrepareAndCast(spell);

            result.IsSuccess.Should().BeTrue();
            result.Value.Message.Should().Contain("Dealt 9 damage"); // 3 darts * 3 damage each
            _target.HitPoints.Received(3).TakeDamage(3, DamageType.Force);
        }

        [Fact]
        public void Execute_MagicMissileStyleSpell_UpcastAddsMoreInstances()
        {
            var spell = new Spell(
                "Magic Missile", 1, SpellSchool.Evocation, "1 Action", "120 feet", "V, S", "Instantaneous", "Three darts.",
                _diceRoller,
                damageRolls: new List<DamageFormula> { new DamageFormula("1d4+1", DamageType.Force) },
                instanceCount: 3, instanceCountPerUpcastLevel: 1);

            _diceRoller.Roll("1d4+1").Returns(Result<DiceRollResult>.Success(new DiceRollResult(3, "1d4+1", new List<int> { 2 }, 1, RollType.Normal)));

            // Cast at slot level 3 (base level 1 + 2 extra) — 3 base darts
            // + 2 extra (one per extra level) = 5 darts.
            var result = PrepareAndCast(spell, slotLevel: 3);

            result.IsSuccess.Should().BeTrue();
            _target.HitPoints.Received(5).TakeDamage(3, DamageType.Force);
        }

        [Fact]
        public void Execute_SingleInstanceSpell_DefaultInstanceCountRollsExactlyOnce()
        {
            // Regression guard: the overwhelming majority of spells (no
            // instanceCount/instanceCountPerUpcastLevel given, so they
            // default to 1/0) must keep rolling damage exactly once, same
            // as before InstanceCount existed at all.
            var spell = new Spell(
                "Inflict Wounds", 1, SpellSchool.Necromancy, "1 Action", "Touch", "V, S", "Instantaneous", "A single necrotic strike.",
                _diceRoller,
                damageRolls: new List<DamageFormula> { new DamageFormula("3d10", DamageType.Necrotic) });

            _diceRoller.Roll("3d10").Returns(Result<DiceRollResult>.Success(new DiceRollResult(15, "3d10", new List<int> { 5, 5, 5 }, 0, RollType.Normal)));

            var result = PrepareAndCast(spell);

            result.IsSuccess.Should().BeTrue();
            _target.HitPoints.Received(1).TakeDamage(15, DamageType.Necrotic);
        }

        [Fact]
        public void Execute_MultiInstanceAttackRollSpell_EachInstanceRollsOwnAttack_OnlyHitsDealDamage()
        {
            // Scorching Ray-style: 3 rays, each with its own attack roll
            // — a miss on one ray must not block or reduce the others.
            var spell = new Spell(
                "Scorching Ray", 2, SpellSchool.Evocation, "1 Action", "120 feet", "V, S", "Instantaneous", "Three rays of fire.",
                _diceRoller, requiresAttackRoll: true,
                damageRolls: new List<DamageFormula> { new DamageFormula("2d6", DamageType.Fire) },
                instanceCount: 3);

            // Target AC 10 (set in constructor). Three attack rolls: hit,
            // miss, hit.
            _diceRoller.Roll("1d20").Returns(
                Result<DiceRollResult>.Success(new DiceRollResult(15, "1d20", new List<int> { 15 }, 0, RollType.Normal)),
                Result<DiceRollResult>.Success(new DiceRollResult(5, "1d20", new List<int> { 5 }, 0, RollType.Normal)),
                Result<DiceRollResult>.Success(new DiceRollResult(18, "1d20", new List<int> { 18 }, 0, RollType.Normal)));
            _diceRoller.Roll("2d6").Returns(Result<DiceRollResult>.Success(new DiceRollResult(7, "2d6", new List<int> { 3, 4 }, 0, RollType.Normal)));

            var result = PrepareAndCast(spell);

            result.IsSuccess.Should().BeTrue();
            result.Value.Message.Should().Contain("Dealt 14 damage"); // 2 hits * 7 damage
            _target.HitPoints.Received(2).TakeDamage(7, DamageType.Fire);
        }
    }
}
