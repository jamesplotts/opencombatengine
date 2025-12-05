using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class JsonMonsterImporterTests
    {
        [Fact]
        public void Import_Should_Parse_Goblin()
        {
            var json = @"
            {
                ""name"": ""Goblin"",
                ""size"": ""S"",
                ""type"": ""humanoid"",
                ""alignment"": ""neutral evil"",
                ""ac"": [
                    15,
                    { ""ac"": 15, ""from"": [""leather armor"", ""shield""] }
                ],
                ""hp"": { ""average"": 7, ""formula"": ""2d6"" },
                ""speed"": { ""walk"": 30 },
                ""str"": 8,
                ""dex"": 14,
                ""con"": 10,
                ""int"": 10,
                ""wis"": 8,
                ""cha"": 8,
                ""action"": [
                    {
                        ""name"": ""Scimitar"",
                        ""entries"": [
                            ""{@hit 4} to hit, reach 5 ft., one target. {@h}5 ({@damage 1d6 + 2}) slashing damage.""
                        ]
                    }
                ]
            }";

            var importer = new JsonMonsterImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var creatures = result.Value.ToList();
            creatures.Should().HaveCount(1);

            var goblin = creatures.First() as StandardCreature;
            goblin.Should().NotBeNull();
            goblin!.Name.Should().Be("Goblin");

            // Stats
            goblin.AbilityScores.Strength.Should().Be(8);
            goblin.AbilityScores.Dexterity.Should().Be(14);

            // HP
            goblin.HitPoints.Max.Should().Be(7);

            // Actions
            var actions = goblin.Actions.ToList();
            var scimitar = actions.OfType<MonsterAttackAction>().FirstOrDefault();
            scimitar.Should().NotBeNull();
            scimitar!.Name.Should().Be("Scimitar");
            scimitar.ToHitBonus.Should().Be(4);
            scimitar.DamageDice.Should().Be("1d6 + 2");
            scimitar.DamageType.Should().Be(DamageType.Slashing);
        }

        [Fact]
        public void Import_Should_Handle_Compendium_Format()
        {
            var json = @"
            {
                ""monster"": [
                    { ""name"": ""Orc"", ""str"": 16, ""dex"": 12, ""con"": 16, ""int"": 7, ""wis"": 11, ""cha"": 10 }
                ]
            }";

            var importer = new JsonMonsterImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value.First().Name.Should().Be("Orc");
        }
    }
}
