using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Implementation.Creatures;
using System.Collections.Generic;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests
{
    public class CheckManagerTests
    {
        [Fact]
        public void RollAbilityCheck_Should_Include_Modifier()
        {
            var abilityScores = Substitute.For<IAbilityScores>();
            abilityScores.GetModifier(Ability.Strength).Returns(3);
            
            var diceRoller = Substitute.For<IDiceRoller>();
            // Fix constructor: Total, Notation, Rolls, Modifier, Type
            diceRoller.Roll("1d20+3").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(13, "1d20+3", new List<int> { 10 }, 3, RollType.Normal)
            ));

            var creature = Substitute.For<ICreature>();
            creature.Effects.Returns((IEffectManager)null);
            creature.AbilityScores.Returns(abilityScores);
            
            var manager = new StandardCheckManager(diceRoller, creature);
            
            var result = manager.RollAbilityCheck(Ability.Strength);

            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(13);
            result.Value.IndividualRolls.Should().Equal(10);
        }

        [Fact]
        public void RollSavingThrow_Should_Include_Modifier()
        {
            var abilityScores = Substitute.For<IAbilityScores>();
            abilityScores.GetModifier(Ability.Dexterity).Returns(2);
            
            var diceRoller = Substitute.For<IDiceRoller>();
            // Fix constructor
            diceRoller.Roll("1d20+2").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(12, "1d20+2", new List<int> { 10 }, 2, RollType.Normal)
            ));

            var creature = Substitute.For<ICreature>();
            creature.Effects.Returns((IEffectManager)null);
            creature.AbilityScores.Returns(abilityScores);
            
            var manager = new StandardCheckManager(diceRoller, creature);
            
            var result = manager.RollSavingThrow(Ability.Dexterity);

            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(12);
            result.Value.IndividualRolls.Should().Equal(10);
        }

        [Fact]
        public void RollAbilityCheck_NegativeModifier_BuildsValidNotationAndSucceeds()
        {
            // Regression test for a real, live-observed bug: naively
            // string-interpolating a negative modifier used to build
            // "1d20+-1" (a double sign), which StandardDiceRoller's own
            // notation regex (a single leading +/- before the digits)
            // rejects outright — the check failed with no roll ever
            // attempted for any creature whose modifier was negative.
            // Confirmed live: a Charisma 8 (-1 modifier, no proficiency)
            // character's Persuasion check came back "resolution_failed".
            var abilityScores = Substitute.For<IAbilityScores>();
            abilityScores.GetModifier(Ability.Charisma).Returns(-1);

            var diceRoller = Substitute.For<IDiceRoller>();
            diceRoller.Roll("1d20-1").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(9, "1d20-1", new List<int> { 10 }, -1, RollType.Normal)
            ));

            var creature = Substitute.For<ICreature>();
            creature.Effects.Returns((IEffectManager)null);
            creature.AbilityScores.Returns(abilityScores);

            var manager = new StandardCheckManager(diceRoller, creature);

            var result = manager.RollAbilityCheck(Ability.Charisma);

            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(9);
        }

        [Fact]
        public void RollSavingThrow_NegativeModifier_BuildsValidNotationAndSucceeds()
        {
            // Same regression as RollAbilityCheck's own negative-modifier
            // case above, for the saving-throw path.
            var abilityScores = Substitute.For<IAbilityScores>();
            abilityScores.GetModifier(Ability.Intelligence).Returns(-2);

            var diceRoller = Substitute.For<IDiceRoller>();
            diceRoller.Roll("1d20-2").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(8, "1d20-2", new List<int> { 10 }, -2, RollType.Normal)
            ));

            var creature = Substitute.For<ICreature>();
            creature.Effects.Returns((IEffectManager)null);
            creature.AbilityScores.Returns(abilityScores);

            var manager = new StandardCheckManager(diceRoller, creature);

            var result = manager.RollSavingThrow(Ability.Intelligence);

            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(8);
        }

        [Fact]
        public void RollAbilityCheck_EffectAdjustsTotal_KeepsIndividualRollsFromRawRoll()
        {
            // A StatBonusEffect (StatType.AbilityCheck) shifts Total after
            // the raw roll, per StandardCheckManager.RollAbilityCheck — the
            // returned DiceRollResult's IndividualRolls must still reflect
            // the actual die shown, not just track whatever Total becomes.
            var abilityScores = Substitute.For<IAbilityScores>();
            abilityScores.GetModifier(Ability.Wisdom).Returns(0);

            var diceRoller = Substitute.For<IDiceRoller>();
            diceRoller.Roll("1d20+0").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(15, "1d20+0", new List<int> { 15 }, 0, RollType.Normal)
            ));

            var effects = Substitute.For<IEffectManager>();
            effects.ApplyStatBonuses(StatType.AbilityCheck, 15).Returns(17); // +2 Guidance-style bonus

            var creature = Substitute.For<ICreature>();
            creature.Effects.Returns(effects);
            creature.AbilityScores.Returns(abilityScores);

            var manager = new StandardCheckManager(diceRoller, creature);

            var result = manager.RollAbilityCheck(Ability.Wisdom);

            result.IsSuccess.Should().BeTrue();
            result.Value.Total.Should().Be(17);
            result.Value.IndividualRolls.Should().Equal(15);
        }
    }
}
