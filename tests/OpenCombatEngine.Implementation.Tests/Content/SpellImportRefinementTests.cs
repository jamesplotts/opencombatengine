using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.Content;
using System.Linq;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class SpellImportRefinementTests
    {
        private readonly JsonSpellImporter _importer;
        private readonly IDiceRoller _diceRoller;

        public SpellImportRefinementTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            _importer = new JsonSpellImporter(_diceRoller);
        }

        [Fact]
        public void Should_Import_Fireball_With_Save_And_Damage()
        {
            var json = @"
            [
                {
                    ""name"": ""Fireball"",
                    ""level"": 3,
                    ""school"": ""E"",
                    ""time"": [{ ""number"": 1, ""unit"": ""action"" }],
                    ""range"": { ""type"": ""point"", ""distance"": { ""type"": ""feet"", ""amount"": 150 } },
                    ""components"": { ""v"": true, ""s"": true, ""m"": ""bat guano"" },
                    ""duration"": [{ ""type"": ""instant"" }],
                    ""entries"": [""A bright streak flashes...""],
                    ""savingThrow"": [""dex""],
                    ""damageInflict"": [""fire""],
                    ""damage"": [[ ""8d6"" ]]
                }
            ]";

            var result = _importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var spell = result.Value.First();

            spell.Name.Should().Be("Fireball");
            spell.RequiresAttackRoll.Should().BeFalse();
            spell.SaveAbility.Should().Be(Ability.Dexterity);
            spell.DamageType.Should().Be(DamageType.Fire);
            spell.DamageDice.Should().Be("8d6");
        }

        [Fact]
        public void Should_Import_Scorching_Ray_With_Attack_And_Damage()
        {
            var json = @"
            [
                {
                    ""name"": ""Scorching Ray"",
                    ""level"": 2,
                    ""school"": ""E"",
                    ""time"": [{ ""number"": 1, ""unit"": ""action"" }],
                    ""range"": { ""type"": ""point"", ""distance"": { ""type"": ""feet"", ""amount"": 120 } },
                    ""components"": { ""v"": true, ""s"": true },
                    ""duration"": [{ ""type"": ""instant"" }],
                    ""entries"": [""You create three rays of fire...""],
                    ""spellAttack"": [""R""],
                    ""damageInflict"": [""fire""],
                    ""damage"": [[ ""2d6"" ]]
                }
            ]";

            var result = _importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var spell = result.Value.First();

            spell.Name.Should().Be("Scorching Ray");
            spell.RequiresAttackRoll.Should().BeTrue();
            spell.SaveAbility.Should().BeNull();
            spell.DamageType.Should().Be(DamageType.Fire);
            spell.DamageDice.Should().Be("2d6");
        }
    }
}
