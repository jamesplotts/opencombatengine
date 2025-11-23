using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Content;
using Xunit;
using System.Linq;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class JsonMagicItemImporterTests
    {
        [Fact]
        public void Import_Should_Parse_Simple_Item()
        {
            var json = @"
            {
                ""name"": ""Potion of Healing"",
                ""type"": ""P"",
                ""rarity"": ""common"",
                ""weight"": 0.5,
                ""entries"": [""You regain 2d4 + 2 hit points.""]
            }";

            var importer = new JsonMagicItemImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var item = result.Value.First();
            item.Name.Should().Be("Potion of Healing");
            item.Type.Should().Be(ItemType.Potion);
            item.RequiresAttunement.Should().BeFalse();
            item.Weight.Should().Be(0.5);
        }

        [Fact]
        public void Import_Should_Parse_Attunement_Item()
        {
            var json = @"
            {
                ""name"": ""Ring of Protection"",
                ""type"": ""RG"",
                ""rarity"": ""rare"",
                ""reqAttune"": true,
                ""entries"": [""You gain a +1 bonus to AC and saving throws.""]
            }";

            var importer = new JsonMagicItemImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var item = result.Value.First();
            item.Name.Should().Be("Ring of Protection");
            item.Type.Should().Be(ItemType.Ring);
            item.RequiresAttunement.Should().BeTrue();
        }

        [Fact]
        public void Import_Should_Parse_Attunement_String()
        {
            var json = @"
            {
                ""name"": ""Wand of the War Mage"",
                ""type"": ""WD"",
                ""reqAttune"": ""by a spellcaster"",
                ""entries"": [""...""]
            }";

            var importer = new JsonMagicItemImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var item = result.Value.First();
            item.RequiresAttunement.Should().BeTrue();
            item.Type.Should().Be(ItemType.Wand);
        }

        [Fact]
        public void Import_Should_Handle_Compendium_Format()
        {
            var json = @"
            {
                ""item"": [
                    { ""name"": ""Item 1"", ""type"": ""M"" },
                    { ""name"": ""Item 2"", ""type"": ""A"" }
                ]
            }";

            var importer = new JsonMagicItemImporter();
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value.First().Name.Should().Be("Item 1");
            result.Value.Last().Name.Should().Be("Item 2");
        }
    }
}
