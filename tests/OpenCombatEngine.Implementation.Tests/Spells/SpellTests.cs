using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class SpellTests
    {
        private readonly OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller _diceRoller = Substitute.For<OpenCombatEngine.Core.Interfaces.Dice.IDiceRoller>();

        [Fact]
        public void Constructor_Should_Set_Properties()
        {
            var spell = new Spell(
                "Fireball", 
                3, 
                SpellSchool.Evocation, 
                "1 Action", 
                "150 feet", 
                "V, S, M", 
                "Instantaneous", 
                "Boom", 
                _diceRoller);

            spell.Name.Should().Be("Fireball");
            spell.Level.Should().Be(3);
            spell.School.Should().Be(SpellSchool.Evocation);
            spell.CastingTime.Should().Be("1 Action");
            spell.Range.Should().Be("150 feet");
            spell.Components.Should().Be("V, S, M");
            spell.Duration.Should().Be("Instantaneous");
            spell.Description.Should().Be("Boom");
        }


    }
}
