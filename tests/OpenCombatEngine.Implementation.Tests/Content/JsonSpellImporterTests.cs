using System.Linq;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.Content;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class JsonSpellImporterTests
    {
        private readonly IDiceRoller _diceRoller = Substitute.For<IDiceRoller>();

        [Fact]
        public void Import_Should_Parse_Single_Spell_Object()
        {
            var json = @"
            {
                ""name"": ""Fireball"",
                ""level"": 3,
                ""school"": ""V"",
                ""time"": [{ ""number"": 1, ""unit"": ""action"" }],
                ""range"": { ""type"": ""point"", ""distance"": { ""type"": ""feet"", ""amount"": 150 } },
                ""components"": { ""v"": true, ""s"": true, ""m"": ""guano"" },
                ""duration"": [{ ""type"": ""instant"" }],
                ""entries"": [""A bright streak flashes...""]
            }";

            var importer = new JsonSpellImporter(_diceRoller);
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var spells = result.Value.ToList();
            spells.Should().HaveCount(1);
            
            var spell = spells.First();
            spell.Name.Should().Be("Fireball");
            spell.Level.Should().Be(3);
            spell.School.Should().Be(SpellSchool.Evocation);
            spell.CastingTime.Should().Be("1 action");
            spell.Range.Should().Be("150 feet");
            spell.Components.Should().Contain("V, S, M (guano)");
            spell.Duration.Should().Be("Instantaneous");
            spell.Description.Should().Contain("A bright streak flashes...");
        }

        [Fact]
        public void Import_Should_Parse_Compendium_Format()
        {
            var json = @"
            {
                ""spell"": [
                    {
                        ""name"": ""Cure Wounds"",
                        ""level"": 1,
                        ""school"": ""E"",
                        ""time"": [{ ""number"": 1, ""unit"": ""action"" }],
                        ""range"": { ""type"": ""touch"" },
                        ""components"": { ""v"": true, ""s"": true },
                        ""duration"": [{ ""type"": ""instant"" }],
                        ""entries"": [""A creature you touch regains...""]
                    }
                ]
            }";

            var importer = new JsonSpellImporter(_diceRoller);
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var spells = result.Value.ToList();
            spells.Should().HaveCount(1);
            spells.First().Name.Should().Be("Cure Wounds");
        }

        [Fact]
        public void Import_Should_Handle_Missing_Optional_Fields()
        {
            var json = @"
            {
                ""name"": ""Minimal Spell"",
                ""level"": 0,
                ""school"": ""A""
            }";

            var importer = new JsonSpellImporter(_diceRoller);
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var spell = result.Value.First();
            spell.Name.Should().Be("Minimal Spell");
            spell.Level.Should().Be(0);
            spell.School.Should().Be(SpellSchool.Abjuration);
            spell.Components.Should().Be("");
        }

        [Fact]
        public void Import_Should_Fail_On_Invalid_Json()
        {
            var json = "{ invalid json }";
            var importer = new JsonSpellImporter(_diceRoller);
            var result = importer.Import(json);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("JSON parsing error");
        }
    }
}
