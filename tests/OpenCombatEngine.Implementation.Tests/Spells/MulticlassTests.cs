using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Classes;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class MulticlassTests
    {
        private class FakeSpellCaster : ISpellCaster
        {
            public Dictionary<int, int> Slots { get; } = new();

            public void SetSlots(int level, int max)
            {
                Slots[level] = max;
            }

            // Implement other members minimally
            public IReadOnlyList<ISpell> KnownSpells => throw new NotImplementedException();
            public IReadOnlyList<ISpell> PreparedSpells => throw new NotImplementedException();
            public Ability CastingAbility => Ability.Intelligence;
            public int SpellSaveDC => 10;
            public int SpellAttackBonus => 0;
            public ISpell? ConcentratingOn => null;
            public bool HasSlot(int level) => false;
            public int GetSlots(int level) => 0;
            public int GetMaxSlots(int level) => Slots.TryGetValue(level, out int v) ? v : 0;
            public Result<bool> ConsumeSlot(int level) => throw new NotImplementedException();
            public void RestoreAllSlots() { }
            public void LearnSpell(ISpell spell) { }
            public Result<bool> PrepareSpell(ISpell spell) => throw new NotImplementedException();
            public void UnprepareSpell(ISpell spell) { }
            public void UnlearnSpell(ISpell spell) { }
            public void SetEffectManager(IEffectManager effects) { }
            public void BreakConcentration() { }
            public void SetConcentration(ISpell spell) { }
        }

        [Fact]
        public void LevelUp_Should_Update_Slots_For_Single_Class_Full_Caster()
        {
            // Arrange
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Gandalf", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: new FakeSpellCaster());
            var levelManager = new StandardLevelManager(creature);
            var wizard = new ClassDefinition("Wizard", 6, spellcastingType: SpellcastingType.Full);

            // Act: Level 1
            levelManager.LevelUp(wizard);

            // Assert
            var caster = (FakeSpellCaster)creature.Spellcasting!;
            caster.Slots[1].Should().Be(2);
            caster.Slots[2].Should().Be(0);

            // Act: Level 3
            levelManager.LevelUp(wizard); // 2
            levelManager.LevelUp(wizard); // 3

            // Assert
            caster.Slots[1].Should().Be(4);
            caster.Slots[2].Should().Be(2);
        }

        [Fact]
        public void LevelUp_Should_Update_Slots_For_Multiclass()
        {
            // Arrange
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "MageKnight", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: new FakeSpellCaster());
            var levelManager = new StandardLevelManager(creature);
            var wizard = new ClassDefinition("Wizard", 6, spellcastingType: SpellcastingType.Full);
            var cleric = new ClassDefinition("Cleric", 8, spellcastingType: SpellcastingType.Full);

            // Act: Wizard 3 / Cleric 2 = Level 5 Caster
            levelManager.LevelUp(wizard);
            levelManager.LevelUp(wizard);
            levelManager.LevelUp(wizard);
            levelManager.LevelUp(cleric);
            levelManager.LevelUp(cleric);

            // Assert
            // Level 5 Caster: 4 Lv1, 3 Lv2, 2 Lv3
            var caster = (FakeSpellCaster)creature.Spellcasting!;
            caster.Slots[1].Should().Be(4);
            caster.Slots[2].Should().Be(3);
            caster.Slots[3].Should().Be(2);
        }

        [Fact]
        public void LevelUp_Should_Handle_Half_Casters()
        {
            // Arrange
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Ranger", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: new FakeSpellCaster());
            var levelManager = new StandardLevelManager(creature);
            var ranger = new ClassDefinition("Ranger", 10, spellcastingType: SpellcastingType.Half);

            // Act: Ranger 5 -> Level 2 Caster (5 / 2 = 2.5 -> 2)
            for(int i=0; i<5; i++) levelManager.LevelUp(ranger);

            // Assert
            // Level 2 Caster: 3 Lv1
            var caster = (FakeSpellCaster)creature.Spellcasting!;
            caster.Slots[1].Should().Be(3);
            caster.Slots[2].Should().Be(0);
        }

        [Fact]
        public void LevelUp_Should_Handle_Mixed_Progression()
        {
            // Arrange
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "PaladinSorcerer", new StandardAbilityScores(10, 10, 10, 10, 10, 10), new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: new FakeSpellCaster());
            var levelManager = new StandardLevelManager(creature);
            var paladin = new ClassDefinition("Paladin", 10, spellcastingType: SpellcastingType.Half);
            var sorcerer = new ClassDefinition("Sorcerer", 6, spellcastingType: SpellcastingType.Full);

            // Act: Paladin 2 (1) + Sorcerer 4 (4) = Level 5 Caster
            // Paladin 2 / 2 = 1
            // Sorcerer 4 = 4
            // Total 5
            levelManager.LevelUp(paladin);
            levelManager.LevelUp(paladin);
            for(int i=0; i<4; i++) levelManager.LevelUp(sorcerer);

            // Assert
            // Level 5 Caster: 4 Lv1, 3 Lv2, 2 Lv3
            var caster = (FakeSpellCaster)creature.Spellcasting!;
            caster.Slots[3].Should().Be(2);
        }
    }
}
