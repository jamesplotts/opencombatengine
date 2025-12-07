using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Implementation.Classes;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Spells
{
    public class SpellListTests
    {
        [Fact]
        public void Service_Should_Validate_Class_Spells()
        {
            // Arrange
            var allowed = new List<string> { "Fireball", "Magic Missile" };
            var spellList = new OpenCombatEngine.Core.Models.Spells.SpellList("Wizard", allowed);
            var classDef = new ClassDefinition("Wizard", 6, null, spellList);
            
            var validSpell = Substitute.For<ISpell>();
            validSpell.Name.Returns("Fireball");
            
            var invalidSpell = Substitute.For<ISpell>();
            invalidSpell.Name.Returns("Cure Wounds");

            // Act & Assert
            SpellValidationService.IsSpellValidForClass(classDef, validSpell).Should().BeTrue();
            SpellValidationService.IsSpellValidForClass(classDef, invalidSpell).Should().BeFalse();
        }

        [Fact]
        public void Service_Should_Return_False_For_Class_Without_List()
        {
            var classDef = new ClassDefinition("Fighter", 10);
            var spell = Substitute.For<ISpell>();
            spell.Name.Returns("Fireball");
            
            SpellValidationService.IsSpellValidForClass(classDef, spell).Should().BeFalse();
        }

        [Fact]
        public void Importer_Should_Parse_SpellList()
        {
            var json = @"
            {
                ""class"": [
                    {
                        ""name"": ""Wizard"",
                        ""hd"": { ""faces"": 6 },
                        ""spells"": [
                            ""Fireball"",
                            ""Magic Missile""
                        ]
                    }
                ]
            }";

            var importer = new JsonClassImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var wizard = result.Value.First();
            wizard.SpellList.Should().NotBeNull();
            wizard.SpellList.Name.Should().Be("Wizard");
            wizard.SpellList.Contains("Fireball").Should().BeTrue();
            wizard.SpellList.Contains("Cure Wounds").Should().BeFalse();
        }
    }
}
