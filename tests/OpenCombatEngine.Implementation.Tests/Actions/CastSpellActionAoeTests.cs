using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Models.Spells;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Spells;
using Xunit;
using System.Collections.Generic;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    public class CastSpellActionAoeTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _caster;
        private readonly IGridManager _grid;
        private readonly ISpellCaster _spellcasting;

        public CastSpellActionAoeTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            _caster = Substitute.For<ICreature>();
            _grid = Substitute.For<IGridManager>();
            _spellcasting = Substitute.For<ISpellCaster>();

            _caster.Name.Returns("Wizard");
            _caster.Spellcasting.Returns(_spellcasting);
        }

        [Fact]
        public void Execute_Should_Hit_Multiple_Targets_In_AOE()
        {
            // Arrange
            var shape = Substitute.For<IShape>();
            var damageRolls = new List<DamageFormula> { new DamageFormula("8d6", DamageType.Fire) };
            var spell = new Spell(
                "Fireball", 3, SpellSchool.Evocation, "1 Action", "150 feet", "V, S, M", "Instantaneous", "Boom",
                _diceRoller, areaOfEffect: shape, damageRolls: damageRolls
            );

            // Prepare spell
            _spellcasting.PreparedSpells.Returns(new List<ISpell> { spell });
            _spellcasting.HasSlot(3).Returns(true);
            _spellcasting.ConsumeSlot(3).Returns(Result<bool>.Success(true));

            // Targets
            var t1 = Substitute.For<ICreature>(); t1.Name.Returns("Goblin 1");
            var t2 = Substitute.For<ICreature>(); t2.Name.Returns("Goblin 2");
            var targets = new List<ICreature> { t1, t2 };

            // Grid Setup
            var targetPos = new Position(10, 10, 0);
            _grid.GetCreaturesInShape(targetPos, shape).Returns(targets);

            // Mock Dice for Damage
            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(20, "8d6", new List<int> { 20 }, 0, RollType.Normal)));

            // Context
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _caster,
                new PositionTarget(targetPos),
                _grid
            );

            var action = new CastSpellAction(spell, diceRoller: _diceRoller);

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Message.Should().Contain("Hits: 2");
            result.Value.Message.Should().Contain("Goblin 1");
            result.Value.Message.Should().Contain("Goblin 2");
            
            // Verify damage taken
            t1.HitPoints.Received(1).TakeDamage(20, DamageType.Fire);
            t2.HitPoints.Received(1).TakeDamage(20, DamageType.Fire);
        }

        [Fact]
        public void Execute_Should_Fail_If_AOE_Spell_Has_No_Target()
        {
            // Arrange
            var shape = Substitute.For<IShape>();
            var spell = new Spell(
                "Fireball", 3, SpellSchool.Evocation, "1 Action", "150 feet", "V, S, M", "Instantaneous", "Boom",
                _diceRoller, areaOfEffect: shape
            );

            // Context with invalid target type (not Creature or Position)
            var invalidTarget = Substitute.For<OpenCombatEngine.Core.Interfaces.Actions.IActionTarget>();
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _caster,
                invalidTarget, 
                _grid
            );

            var action = new CastSpellAction(spell);

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("AOE spell requires a target");
        }
    }
}
