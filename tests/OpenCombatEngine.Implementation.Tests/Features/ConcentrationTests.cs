using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class ConcentrationTests
    {
        private readonly StandardCreature _creature;
        private readonly ISpell _concentrationSpell;
        private readonly ISpell _instantSpell;
        private readonly IDiceRoller _diceRoller;

        public ConcentrationTests()
        {
            // StandardAbilityScores is immutable.
            var abilityScores = new StandardAbilityScores(constitution: 10);
            
            _diceRoller = Substitute.For<IDiceRoller>();
            var hp = new StandardHitPoints(20, combatStats: null, diceRoller: _diceRoller);
            
            var mockCreature = Substitute.For<ICreature>();
            mockCreature.AbilityScores.Returns(abilityScores);
            mockCreature.ProficiencyBonus.Returns(2);
            
            var spellCaster = new StandardSpellCaster(mockCreature, Ability.Intelligence, isPreparedCaster: true);
            
            _creature = new StandardCreature(System.Guid.NewGuid().ToString(), "Caster", abilityScores, hp, new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster);

            _concentrationSpell = Substitute.For<ISpell>();
            _concentrationSpell.Name.Returns("Haste");
            _concentrationSpell.RequiresConcentration.Returns(true);
            _concentrationSpell.Level.Returns(1);

            _instantSpell = Substitute.For<ISpell>();
            _instantSpell.Name.Returns("Fireball");
            _instantSpell.RequiresConcentration.Returns(false);
            _instantSpell.Level.Returns(1);
            
            // Give slots
            spellCaster.SetSlots(1, 4);
            spellCaster.RestoreAllSlots();
        }

        [Fact]
        public void Casting_Concentration_Spell_Should_Set_Concentration()
        {
            // Mock preparation
            ((StandardSpellCaster)_creature.Spellcasting!).LearnSpell(_concentrationSpell);
            ((StandardSpellCaster)_creature.Spellcasting!).PrepareSpell(_concentrationSpell);
            
            var action = new CastSpellAction(_concentrationSpell);
            var context = new StandardActionContext(_creature, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_creature));
            
            // Mock cast result
            _concentrationSpell.Cast(Arg.Any<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>(), Arg.Any<object>())
                .Returns(OpenCombatEngine.Core.Results.Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(new OpenCombatEngine.Core.Models.Spells.SpellResolution(true, "Cast")));

            action.Execute(context);

            _creature.Spellcasting.ConcentratingOn.Should().Be(_concentrationSpell);
        }

        [Fact]
        public void Casting_Second_Concentration_Spell_Should_Break_First()
        {
            // Prepare both
            ((StandardSpellCaster)_creature.Spellcasting!).LearnSpell(_concentrationSpell);
            ((StandardSpellCaster)_creature.Spellcasting!).PrepareSpell(_concentrationSpell);
            
            var secondConc = Substitute.For<ISpell>();
            secondConc.Name.Returns("Fly");
            secondConc.RequiresConcentration.Returns(true);
            secondConc.Level.Returns(1);
            
            ((StandardSpellCaster)_creature.Spellcasting!).LearnSpell(secondConc);
            ((StandardSpellCaster)_creature.Spellcasting!).PrepareSpell(secondConc);
            
            // Cast first
            _creature.Spellcasting.SetConcentration(_concentrationSpell);
            _creature.Spellcasting.ConcentratingOn.Should().Be(_concentrationSpell);

            // Cast second
            var action = new CastSpellAction(secondConc);
            var context = new StandardActionContext(_creature, new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_creature));
            
            secondConc.Cast(Arg.Any<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>(), Arg.Any<object>())
                .Returns(OpenCombatEngine.Core.Results.Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(new OpenCombatEngine.Core.Models.Spells.SpellResolution(true, "Cast")));

            action.Execute(context);

            _creature.Spellcasting.ConcentratingOn.Should().Be(secondConc);
        }

        [Fact]
        public void Taking_Damage_Should_Trigger_Save_And_Break_On_Failure()
        {
            _creature.Spellcasting!.SetConcentration(_concentrationSpell);
            
            var mockCheckManager = Substitute.For<ICheckManager>();
            var abilityScores = new StandardAbilityScores();
            
            var mockCreature = Substitute.For<ICreature>();
            mockCreature.AbilityScores.Returns(abilityScores);
            mockCreature.ProficiencyBonus.Returns(2);
            
            var spellCaster = new StandardSpellCaster(mockCreature, Ability.Intelligence);
            var creature = new StandardCreature(System.Guid.NewGuid().ToString(), "Caster2", abilityScores, new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster, checkManager: mockCheckManager);
            
            creature.Spellcasting!.SetConcentration(_concentrationSpell);
            
            // Mock Save Failure
            mockCheckManager.RollSavingThrow(Ability.Constitution).Returns(OpenCombatEngine.Core.Results.Result<int>.Success(5)); // Fail DC 10
            
            creature.HitPoints.TakeDamage(20);
            
            mockCheckManager.Received().RollSavingThrow(Ability.Constitution);
            
            creature.Spellcasting.ConcentratingOn.Should().BeNull();
        }

        [Fact]
        public void Taking_Damage_Should_Maintain_Concentration_On_Success()
        {
            var mockCheckManager = Substitute.For<ICheckManager>();
            var abilityScores = new StandardAbilityScores();
            
            var mockCreature = Substitute.For<ICreature>();
            mockCreature.AbilityScores.Returns(abilityScores);
            mockCreature.ProficiencyBonus.Returns(2);
            
            var spellCaster = new StandardSpellCaster(mockCreature, Ability.Intelligence);
            var creature = new StandardCreature(System.Guid.NewGuid().ToString(), "Caster3", abilityScores, new StandardHitPoints(20), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()), spellcasting: spellCaster, checkManager: mockCheckManager);
            
            creature.Spellcasting!.SetConcentration(_concentrationSpell);
            
            // Mock Save Success
            mockCheckManager.RollSavingThrow(Ability.Constitution).Returns(OpenCombatEngine.Core.Results.Result<int>.Success(15)); // Pass DC 10
            
            creature.HitPoints.TakeDamage(20);
            
            creature.Spellcasting.ConcentratingOn.Should().Be(_concentrationSpell);
        }
    }
}
