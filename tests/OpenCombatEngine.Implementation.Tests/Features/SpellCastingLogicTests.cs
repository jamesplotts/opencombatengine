using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class SpellCastingLogicTests
    {
        [Fact]
        public void ConditionFactory_Should_Parse_Duration_Correctly()
        {
            ConditionFactory.Create("Test", "1 minute").DurationRounds.Should().Be(10);
            ConditionFactory.Create("Test", "1 round").DurationRounds.Should().Be(1);
            ConditionFactory.Create("Test", "1 hour").DurationRounds.Should().Be(600);
            ConditionFactory.Create("Test", "Instantaneous").DurationRounds.Should().Be(0);
            ConditionFactory.Create("Test", "10 minutes").DurationRounds.Should().Be(100);
        }

        [Fact]
        public void ConditionFactory_Should_Parse_Type_Correctly()
        {
            ConditionFactory.Create("Blinded", "1 round").Type.Should().Be(ConditionType.Blinded);
            ConditionFactory.Create("Charmed", "1 round").Type.Should().Be(ConditionType.Charmed);
            ConditionFactory.Create("UnknownCondition", "1 round").Type.Should().Be(ConditionType.Custom);
        }

        [Fact]
        public void SpellCaster_Should_Prioritize_Standard_Slots()
        {
            // Arrange
            var caster = new StandardSpellCaster(Ability.Intelligence, a => 0, () => 2);
            caster.SetSlots(1, 4);
            caster.SetPactSlots(2, 1); // 2 level 1 pact slots

            // Act - Consume 4 level 1 slots (Standard)
            for(int i = 0; i < 4; i++)
            {
                var result = caster.ConsumeSlot(1);
                result.IsSuccess.Should().BeTrue();
            }

            // Assert
            caster.GetSlots(1).Should().Be(2); // Should have 0 standard left, but 2 pact slots visible
            // Standard slots count logic: GetSlots returns standard + relevant pact.
            // If we consumed 4 standard, we have 0 standard.
            // Pact slots are level 1, so they add to GetSlots(1).
            // So total remaining should be 2.
            
            // Verify internal state implications
            // Consume one more -> should take pact slot
            caster.ConsumeSlot(1).IsSuccess.Should().BeTrue();
            caster.PactSlotsCurrent.Should().Be(1);
        }

        [Fact]
        public void SpellCaster_Should_Use_Pact_Slots_For_Lower_Level_Spells()
        {
            // Arrange
            var caster = new StandardSpellCaster(Ability.Charisma, a => 3, () => 2);
            caster.SetSlots(1, 0); // No standard slots
            caster.SetPactSlots(2, 3); // 2 level 3 pact slots

            // Act - Cast level 1 spell (consume level 1 slot)
            // Should be able to use level 3 pact slot
            var result = caster.ConsumeSlot(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            caster.PactSlotsCurrent.Should().Be(1);
        }

        [Fact]
        public void SpellCaster_Should_Fail_If_No_High_Level_Slots()
        {
            // Arrange
            var caster = new StandardSpellCaster(Ability.Wisdom, a => 0, () => 2);
            caster.SetSlots(1, 4);
            caster.SetPactSlots(0, 0);

            // Act
            var result = caster.ConsumeSlot(2);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void SpellCaster_Calculates_DC_Correctly()
        {
            // Arrange
            // Int 16 (+3), PB +2 -> DC = 8 + 3 + 2 = 13
            var caster = new StandardSpellCaster(Ability.Intelligence, 
                a => a == Ability.Intelligence ? 3 : 0, 
                () => 2);

            // Assert
            caster.SpellSaveDC.Should().Be(13);
        }
        
        [Fact]
        public void SpellCaster_Calculates_AttackBonus_Correctly()
        {
            // Arrange
            // Int 16 (+3), PB +2 -> Bonus = 3 + 2 = +5
            var caster = new StandardSpellCaster(Ability.Intelligence, 
                a => a == Ability.Intelligence ? 3 : 0, 
                () => 2);

            // Assert
            caster.SpellAttackBonus.Should().Be(5);
        }
    }
}
