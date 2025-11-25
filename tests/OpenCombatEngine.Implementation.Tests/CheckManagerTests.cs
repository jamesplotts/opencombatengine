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
            
            var manager = new StandardCheckManager(abilityScores, diceRoller, creature);
            
            var result = manager.RollAbilityCheck(Ability.Strength);
            
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(13);
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
            
            var manager = new StandardCheckManager(abilityScores, diceRoller, creature);
            
            var result = manager.RollSavingThrow(Ability.Dexterity);
            
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(12);
        }
    }
}
