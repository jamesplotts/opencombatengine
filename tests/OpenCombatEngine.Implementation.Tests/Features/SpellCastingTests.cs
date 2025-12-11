using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Models.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.Actions.Spells;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spells;
using OpenCombatEngine.Implementation.Spatial;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class SpellCastingTests
    {
        private class FakeSpell : ISpell
        {
            public string Name { get; }
            public int Level { get; }
            public SpellSchool School { get; } = SpellSchool.Evocation;
            public string CastingTime { get; } = "1 Action";
            public string Range { get; } = "60 ft";
            public string Components { get; } = "V, S";
            public string Duration { get; } = "Instantaneous";
            public string Description { get; } = "A fake spell.";
            public bool RequiresAttackRoll { get; } = false;
            public bool RequiresConcentration { get; } = false;
            public bool Ritual { get; } = false; // Not in ISpell? Wait ISpell doesn't check Ritual.
            // Let's check ISpell interface from Step 1033. It DOES NOT have Ritual property?
            // Line 1-41 in Step 1033. No Ritual.
            // So remove Ritual.

            public IShape? AreaOfEffect { get; } = null;
            public Ability? SaveAbility { get; } = null;
            public SaveEffect SaveEffect { get; } = SaveEffect.None;
            
            public System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.DamageFormula> DamageRolls => new System.Collections.Generic.List<OpenCombatEngine.Core.Models.Spells.DamageFormula>();
            public string? HealingDice => null;
            public System.Collections.Generic.IReadOnlyList<OpenCombatEngine.Core.Models.Spells.SpellConditionDefinition> AppliedConditions => new System.Collections.Generic.List<OpenCombatEngine.Core.Models.Spells.SpellConditionDefinition>();

            public FakeSpell(string name, int level)
            {
                Name = name;
                Level = level;
            }
            
            public Result<SpellResolution> Cast(OpenCombatEngine.Core.Interfaces.Creatures.ICreature caster, object? target = null)
            {
                return Result<SpellResolution>.Success(new SpellResolution(true, $"Casted {Name}"));
            }
        }

        private StandardCreature CreateCaster()
        {
            // FakeDiceRoller logic not needed strictly here unless we test rolls, but StandardCreature needs ITurnManager with dice.
            var creature = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Wizard",
                new StandardAbilityScores(10, 10, 10, 16, 10, 10), // Int 16 (+3)
                new StandardHitPoints(100),
                new StandardInventory(),
                new StandardTurnManager(new OpenCombatEngine.Implementation.Dice.StandardDiceRoller())
            );
            
            // Add Spellcasting
            var spellCaster = new StandardSpellCaster(Ability.Intelligence, 
                a => creature.AbilityScores.GetModifier(a), 
                () => creature.ProficiencyBonus);
                
            creature.SetSpellCaster(spellCaster);
            return creature;
        }

        [Fact]
        public void SpellCaster_should_track_slots()
        {
            var creature = CreateCaster();
            var caster = creature.Spellcasting!;

            caster.SetSlots(1, 2); // 2 Level 1 slots
            
            caster.HasSlot(1).Should().BeTrue();
            caster.GetSlots(1).Should().Be(2);
            
            caster.ConsumeSlot(1).IsSuccess.Should().BeTrue();
            caster.GetSlots(1).Should().Be(1);
            
            caster.ConsumeSlot(1).IsSuccess.Should().BeTrue();
            caster.GetSlots(1).Should().Be(0);
            
            caster.ConsumeSlot(1).IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void SpellCaster_should_calculate_dc()
        {
            var creature = CreateCaster();
            var caster = creature.Spellcasting!;
            
            // Int 16 (+3). PB (likely 2 for lvl 1/new creature).
            // DC = 8 + 3 + 2 = 13.
            
            caster.SpellSaveDC.Should().Be(13);
        }

        [Fact]
        public void CastSpellAction_should_consume_slot()
        {
            var creature = CreateCaster();
            var caster = creature.Spellcasting!;
            caster.SetSlots(1, 1);
            
            var spell = new FakeSpell("Magic Missile", 1);
            caster.LearnSpell(spell);
            caster.PrepareSpell(spell);
            
            var action = new CastSpellAction(spell);
            var target = CreateCaster(); // Dummy target
            var context = new StandardActionContext(creature, new CreatureTarget(target));
            
            // Act
            var result = action.Execute(context);
            
            // Assert
            result.IsSuccess.Should().BeTrue();
            caster.GetSlots(1).Should().Be(0);
        }
        
        [Fact]
        public void CastSpellAction_should_fail_if_not_prepared()
        {
            var creature = CreateCaster();
            var caster = creature.Spellcasting!;
            caster.SetSlots(1, 1);
            
            var spell = new FakeSpell("Magic Missile", 1);
            caster.LearnSpell(spell);
            // Not prepared
            
            var action = new CastSpellAction(spell);
            var target = CreateCaster();
            var context = new StandardActionContext(creature, new CreatureTarget(target));
            
            // Act
            var result = action.Execute(context);
            
            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not prepared");
        }
    }
}
