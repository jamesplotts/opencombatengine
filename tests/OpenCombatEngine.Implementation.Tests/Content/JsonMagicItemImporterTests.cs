using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Implementation.Content;
using OpenCombatEngine.Implementation.Items;
using Xunit;
using System.Linq;

namespace OpenCombatEngine.Implementation.Tests.Content
{
    public class JsonMagicItemImporterTests
    {
        private readonly ISpellRepository _spellRepository;

        public JsonMagicItemImporterTests()
        {
            _spellRepository = Substitute.For<ISpellRepository>();
        }

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

            var importer = new JsonMagicItemImporter(_spellRepository);
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

            var importer = new JsonMagicItemImporter(_spellRepository);
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

            var importer = new JsonMagicItemImporter(_spellRepository);
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

            var importer = new JsonMagicItemImporter(_spellRepository);
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value.First().Name.Should().Be("Item 1");
            result.Value.Last().Name.Should().Be("Item 2");
        }

        [Fact]
        public void Import_Should_Parse_Recharge_Logic()
        {
            var json = @"
            {
                ""name"": ""Staff of Power"",
                ""type"": ""ST"",
                ""charges"": 20,
                ""recharge"": ""2d8 + 4 at dawn"",
                ""entries"": [""...""]
            }";

            var importer = new JsonMagicItemImporter(_spellRepository);
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var item = result.Value.First();
            item.Charges.Should().Be(20);
            item.MaxCharges.Should().Be(20);
            item.RechargeFrequency.Should().Be(RechargeFrequency.Dawn);
            item.RechargeFormula.Should().Be("2D8+4");
        }

        [Fact]
        public void Import_Should_Parse_Attached_Spells()
        {
            var json = @"
            {
                ""name"": ""Staff of Fire"",
                ""type"": ""ST"",
                ""charges"": 10,
                ""recharge"": ""1d6 + 4 at dawn"",
                ""attachedSpells"": [""Fireball"", ""Burning Hands""],
                ""entries"": [""...""]
            }";

            var importer = new JsonMagicItemImporter(_spellRepository);
            var result = importer.Import(json);

            result.IsSuccess.Should().BeTrue();
            var item = result.Value.First();
            item.Abilities.Should().HaveCount(2);
            
            var fireball = item.Abilities.First();
            fireball.Should().BeOfType<CastSpellFromItemAbility>();
            fireball.Name.Should().Contain("Fireball");
        }
    }
}
