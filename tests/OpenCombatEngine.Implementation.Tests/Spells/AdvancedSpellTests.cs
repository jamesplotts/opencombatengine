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
using OpenCombatEngine.Implementation.Effects;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class AdvancedSpellTests
    {
        [Fact]
        public void CastSpellAction_Should_Scale_Cantrip_Damage()
        {
            // Arrange
            // Create Level 5 Caster
            var spellCaster = new StandardSpellCaster(Ability.Intelligence, a => 0, () => 2, false);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Mage", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster);
            var levelManager = creature.LevelManager;
            var wizard = new ClassDefinition("Wizard", 6, spellcastingType: SpellcastingType.Full);
            
            for(int i=0; i<5; i++) levelManager.LevelUp(wizard); // Level 5

            // spell: Fire Bolt (Level 0, 1d10)
            // Mock Dice: Returns 5 always.
            var diceRoller = Substitute.For<IDiceRoller>();
            diceRoller.Roll(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<DiceRollResult>.Success(new DiceRollResult(5, "1d10", new List<int>{5}, 0, RollType.Normal)));

            var fireBolt = Substitute.For<ISpell>();
            fireBolt.Name.Returns("Fire Bolt");
            fireBolt.Level.Returns(0);
            fireBolt.DamageRolls.Returns(new List<OpenCombatEngine.Core.Models.Spells.DamageFormula> { new OpenCombatEngine.Core.Models.Spells.DamageFormula("1d10", DamageType.Fire) });
            fireBolt.RequiresConcentration.Returns(false);
            fireBolt.AppliedConditions.Returns(new List<OpenCombatEngine.Core.Models.Spells.SpellConditionDefinition>()); // Mock required property
            
            // Mock Cast
            fireBolt.Cast(Arg.Any<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>(), Arg.Any<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>())
                .Returns(OpenCombatEngine.Core.Results.Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(new SpellResolution(true, "Hit")));

            spellCaster.LearnSpell(fireBolt);

            var action = new CastSpellAction(fireBolt, diceRoller: diceRoller);
            var context = Substitute.For<OpenCombatEngine.Core.Interfaces.Actions.IActionContext>();
            context.Source.Returns(creature);
            // Self target
            context.Target.Returns(new OpenCombatEngine.Core.Models.Actions.CreatureTarget(creature));

            // Act
            action.Execute(context);

            // Assert
            // Should roll twice (Level >= 5).
            // Expect TakeDamage call with 5 * 2 = 10? No, TakeDamage called for EACH roll in my logic loop.
            // Wait, my impl loops and calls TakeDamage.
            // So: TakeDamage(5), TakeDamage(5).
            // Total damage 10 effectively.
            
            creature.HitPoints.Current.Should().BeLessThan(creature.HitPoints.Max); // Took damage
            
            // Verify dice rolled twice
            diceRoller.Received(2).Roll("1d10");
        }

        [Fact]
        public void Warlock_Should_Have_Pact_Slots()
        {
            // Arrange
            var spellCaster = new StandardSpellCaster(Ability.Charisma, a => 0, () => 2, false);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Warlock", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster);
            var levelManager = new StandardLevelManager(creature);
            var warlock = new ClassDefinition("Warlock", 8, spellcastingType: SpellcastingType.Pact);

            // Act: Level 1
            levelManager.LevelUp(warlock);

            // Assert: 1 slot, 1st level
            spellCaster.PactSlotsMax.Should().Be(1);
            spellCaster.PactSlotLevel.Should().Be(1);
            spellCaster.HasSlot(1).Should().BeTrue();

            // Act: Level 2
            levelManager.LevelUp(warlock);
            // 2 slots, 1st level
            spellCaster.PactSlotsMax.Should().Be(2);

            // Act: Level 3
            levelManager.LevelUp(warlock);
            // 2 slots, 2nd level
            spellCaster.PactSlotsMax.Should().Be(2);
            spellCaster.PactSlotLevel.Should().Be(2);
            
            // Verify HasSlot(2) is true
            spellCaster.HasSlot(2).Should().BeTrue();
            // Verify HasSlot(1) is true (because PactSlotLevel >= 1)
            spellCaster.HasSlot(1).Should().BeTrue();
        }

        [Fact]
        public void Pact_Slots_Should_Recover_On_Short_Rest()
        {
            // Arrange
            var spellCaster = new StandardSpellCaster(Ability.Charisma, a => 0, () => 2, false);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Warlock", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster);
            var levelManager = new StandardLevelManager(creature);
            var warlock = new ClassDefinition("Warlock", 8, spellcastingType: SpellcastingType.Pact);
            levelManager.LevelUp(warlock);

            // Consume Slot
            // Use explicit method or CastSpellAction? 
            // StandardSpellCaster.ConsumeSlot(1) should use Pact Slot if no regular slots.
            // Since pure Warlock has 0 regular slots.
            
            spellCaster.PactSlotsCurrent.Should().Be(1);
            spellCaster.ConsumeSlot(1).IsSuccess.Should().BeTrue();
            spellCaster.PactSlotsCurrent.Should().Be(0);
            
            // Act: Short Rest
            creature.Rest(RestType.ShortRest);

            // Assert
            spellCaster.PactSlotsCurrent.Should().Be(1);
        }
    }
}
