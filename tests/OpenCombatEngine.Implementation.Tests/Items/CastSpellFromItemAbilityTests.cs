using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Items
{
    public class CastSpellFromItemAbilityTests
    {
        private readonly ISpellRepository _spellRepository;
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _user;
        private readonly IActionContext _context;
        private readonly ISpell _spell;

        public CastSpellFromItemAbilityTests()
        {
            _spellRepository = Substitute.For<ISpellRepository>();
            _diceRoller = Substitute.For<IDiceRoller>();
            _user = Substitute.For<ICreature>();
            _context = Substitute.For<IActionContext>();
            _spell = Substitute.For<ISpell>();
            
            _context.Source.Returns(_user);
            _spell.Name.Returns("Fireball");
            _spell.Level.Returns(3);
        }

        [Fact]
        public void Execute_Should_Fail_If_Spell_Not_Found()
        {
            // Arrange
            _spellRepository.GetSpell("Fireball").Returns(Result<ISpell>.Failure("Not found"));
            var ability = new CastSpellFromItemAbility(_spellRepository, "Fireball", 1, _diceRoller);

            // Act
            var result = ability.Execute(_user, _context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("not found");
        }

        [Fact]
        public void Execute_Should_Cast_Spell_If_Found()
        {
            // Arrange
            _spellRepository.GetSpell("Fireball").Returns(Result<ISpell>.Success(_spell));
            _spell.Cast(Arg.Any<ICreature>(), Arg.Any<ICreature>()).Returns(Result<OpenCombatEngine.Core.Models.Spells.SpellResolution>.Success(new OpenCombatEngine.Core.Models.Spells.SpellResolution(true, "Cast success")));
            
            // Mock targeting context for single target spell (simplest case)
            var targetCreature = Substitute.For<ICreature>();
            _context.Target.Returns(new CreatureTarget(targetCreature));
            
            var ability = new CastSpellFromItemAbility(_spellRepository, "Fireball", 1, _diceRoller);

            // Act
            var result = ability.Execute(_user, _context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _spell.Received().Cast(_user, targetCreature);
        }
    }
}
