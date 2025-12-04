using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Content;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class JsonRaceImporterTests
    {
        [Fact]
        public void Import_Should_Parse_Valid_Race_Json()
        {
            var json = @"
            {
                ""race"": [
                    {
                        ""name"": ""Human"",
                        ""source"": ""PHB"",
                        ""size"": ""M"",
                        ""speed"": 30,
                        ""ability"": [
                            {
                                ""str"": 1,
                                ""dex"": 1,
                                ""con"": 1,
                                ""int"": 1,
                                ""wis"": 1,
                                ""cha"": 1
                            }
                        ]
                    }
                ]
            }";

            var importer = new JsonRaceImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var races = result.Value.ToList();

            races.Should().HaveCount(1);
            var human = races.First();
            human.Name.Should().Be("Human");
            human.Size.Should().Be(Size.Medium);
            human.Speed.Should().Be(30);
            human.AbilityScoreIncreases.Should().Contain(Ability.Strength, 1);
            human.AbilityScoreIncreases.Should().Contain(Ability.Intelligence, 1);
        }

        [Fact]
        public void Import_Should_Parse_Complex_Speed_And_Size()
        {
            var json = @"
            {
                ""race"": [
                    {
                        ""name"": ""Halfling"",
                        ""size"": [""S""],
                        ""speed"": { ""walk"": 25 },
                        ""ability"": [
                            { ""dex"": 2 }
                        ]
                    }
                ]
            }";

            var importer = new JsonRaceImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var races = result.Value.ToList();

            races.Should().HaveCount(1);
            var halfling = races.First();
            halfling.Name.Should().Be("Halfling");
            halfling.Size.Should().Be(Size.Small);
            halfling.Speed.Should().Be(25);
            halfling.AbilityScoreIncreases.Should().Contain(Ability.Dexterity, 2);
        }

        [Fact]
        public void Import_Should_Ignore_Choose_Ability()
        {
            var json = @"
            {
                ""race"": [
                    {
                        ""name"": ""Half-Elf"",
                        ""ability"": [
                            { ""cha"": 2 },
                            { ""choose"": { ""from"": [""str"", ""dex""], ""count"": 2 } }
                        ]
                    }
                ]
            }";

            var importer = new JsonRaceImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var races = result.Value.ToList();

            races.Should().HaveCount(1);
            var halfElf = races.First();
            halfElf.Name.Should().Be("Half-Elf");
            halfElf.AbilityScoreIncreases.Should().Contain(Ability.Charisma, 2);
            // Should not crash on 'choose'
        }
    }
}
