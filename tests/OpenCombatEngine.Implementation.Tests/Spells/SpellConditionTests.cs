using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Spells;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Classes;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Spells;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class SpellConditionTests
    {
        [Fact]
        public void CastSpell_Should_Apply_Condition_On_Failed_Save()
        {
            // Arrange
            var spellCaster = new StandardSpellCaster(Ability.Intelligence, a => 0, () => 2, isPreparedCaster: false);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Caster", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster);
            
            // Mock Dice for Verify Save Failure
            var diceRoller = Substitute.For<IDiceRoller>();
            // Saving Throw (d20). DC = 8+2+0 = 10.
            // Roll 1 => Total 1 (1+0) => Fails.
            diceRoller.Roll(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<DiceRollResult>.Success(new DiceRollResult(1, "1d20", new List<int>{1}, 0, RollType.Normal)));

            var targetStats = new StandardAbilityScores(10, 10, 10, 10, 10, 10);
            var target = new StandardCreature(Guid.NewGuid().ToString(), "Target", targetStats, new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), defaultDiceRoller: diceRoller);

            // Spell: Hold Person (Paralyzed, Save Wis, Negate)
            var conditions = new List<SpellConditionDefinition>
            {
                new SpellConditionDefinition("Paralyzed", "1 minute", SaveEffect.Negate)
            };
            
            var holdPerson = new Spell(
                "Hold Person", 
                2, 
                SpellSchool.Enchantment, 
                "1 action", 
                "60 feet", 
                "V, S, M", 
                "1 minute", 
                "Paralyzes target",
                new StandardDiceRoller(),
                saveAbility: Ability.Wisdom,
                saveEffect: SaveEffect.Negate,
                appliedConditions: conditions
            );
            
            spellCaster.LearnSpell(holdPerson);
            spellCaster.SetSlots(2, 1);

            var action = new CastSpellAction(holdPerson, diceRoller: diceRoller);
            var context = Substitute.For<OpenCombatEngine.Core.Interfaces.Actions.IActionContext>();
            context.Source.Returns(creature);
            context.Target.Returns(new OpenCombatEngine.Core.Models.Actions.CreatureTarget(target));

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // Target should have Paralyzed condition
            // HasCondition takes Enum, but Paralyzed might be custom string name via Generic Condition.
            // Check ActiveConditions for name match.
            result.Value.Message.Should().Contain("Applied Paralyzed");
            // Also check condition presence if message says applied
            System.Linq.Enumerable.Any(target.Conditions.ActiveConditions, c => c.Name == "Paralyzed").Should().BeTrue();
        }

        [Fact]
        public void CastSpell_Should_Not_Apply_Condition_On_Success_Save()
        {
            // ... (Arrange omitted for brevity, logic similar) ...
            // Arrange (duplicate setup mostly)
            var spellCaster = new StandardSpellCaster(Ability.Intelligence, a => 0, () => 2, isPreparedCaster: false);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Caster", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster);
            
            // Mock Dice for Success
            var diceRoller = Substitute.For<IDiceRoller>();
            // DC 10. Roll 15 + mod(5) = 20 => Success.
            diceRoller.Roll(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<DiceRollResult>.Success(new DiceRollResult(15, "1d20", new List<int>{15}, 0, RollType.Normal)));

            var target = new StandardCreature(Guid.NewGuid().ToString(), "Target", new StandardAbilityScores(10, 10, 10, 10, 20, 10), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), defaultDiceRoller: diceRoller); // High Wis

            // Spell: Hold Person (Paralyzed, Save Wis, Negate)
            var conditions = new List<SpellConditionDefinition>
            {
                new SpellConditionDefinition("Paralyzed", "1 minute", SaveEffect.Negate)
            };
            
            var holdPerson = new Spell(
                "Hold Person", 
                2, 
                SpellSchool.Enchantment, 
                "1 action", 
                "60 feet", 
                "V, S, M", 
                "1 minute", 
                "Paralyzes target",
                new StandardDiceRoller(),
                saveAbility: Ability.Wisdom,
                saveEffect: SaveEffect.Negate,
                appliedConditions: conditions
            );
            
            spellCaster.LearnSpell(holdPerson);
            spellCaster.SetSlots(2, 1);

            var action = new CastSpellAction(holdPerson, diceRoller: diceRoller);
            var context = Substitute.For<OpenCombatEngine.Core.Interfaces.Actions.IActionContext>();
            context.Source.Returns(creature);
            context.Target.Returns(new OpenCombatEngine.Core.Models.Actions.CreatureTarget(target));

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
             System.Linq.Enumerable.Any(target.Conditions.ActiveConditions, c => c.Name == "Paralyzed").Should().BeFalse();
        }
    }
}
