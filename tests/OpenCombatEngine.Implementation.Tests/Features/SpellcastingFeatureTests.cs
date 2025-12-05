using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Implementation.Features;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class SpellcastingFeatureTests
    {
        [Fact]
        public void OnApplied_Should_Learn_Spells()
        {
            // Arrange
            var spell = Substitute.For<ISpell>();
            spell.Name.Returns("Fireball");
            var feature = new SpellcastingFeature("Fireball Feature", new[] { spell });
            
            var spellCaster = Substitute.For<ISpellCaster>();
            var creature = Substitute.For<ICreature>();
            creature.Spellcasting.Returns(spellCaster);

            // Act
            feature.OnApplied(creature);

            // Assert
            spellCaster.Received(1).LearnSpell(spell);
        }

        [Fact]
        public void OnRemoved_Should_Unlearn_Spells()
        {
            // Arrange
            var spell = Substitute.For<ISpell>();
            spell.Name.Returns("Fireball");
            var feature = new SpellcastingFeature("Fireball Feature", new[] { spell });
            
            var spellCaster = Substitute.For<ISpellCaster>();
            var creature = Substitute.For<ICreature>();
            creature.Spellcasting.Returns(spellCaster);

            // Act
            feature.OnRemoved(creature);

            // Assert
            spellCaster.Received(1).UnlearnSpell(spell);
        }
    }
}
