using System.Linq;
using System.Net.Http;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Open5e;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Items
{
    public class StandardItemLibraryTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly Open5eContentSource _contentSource;

        public StandardItemLibraryTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            var client = Substitute.For<Open5eClient>(new HttpClient());
            _contentSource = new Open5eContentSource(client, _diceRoller);
        }

        [Fact]
        public void ImportMagicItemsFromJson_Should_Add_Items_With_Full_Fidelity()
        {
            var library = new StandardItemLibrary(_contentSource, _diceRoller);
            var json = @"
            {
                ""name"": ""Staff of Fire"",
                ""type"": ""ST"",
                ""rarity"": ""very rare"",
                ""reqAttune"": true,
                ""charges"": 10,
                ""recharge"": ""1d6 + 4 at dawn"",
                ""attachedSpells"": [""Fireball""],
                ""entries"": [""A gnarled black staff...""]
            }";

            var result = library.ImportMagicItemsFromJson(json);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();

            var resolved = library.GetItem("Staff of Fire");
            resolved.Should().BeAssignableTo<IMagicItem>();
            var magicItem = (IMagicItem)resolved!;

            magicItem.RequiresAttunement.Should().BeTrue();
            magicItem.MaxCharges.Should().Be(10);
            magicItem.RechargeFrequency.Should().Be(RechargeFrequency.Dawn);
            magicItem.RechargeFormula.Should().Be("1D6+4");
            magicItem.Abilities.Should().ContainSingle(a => a.Name.Contains("Fireball"));
        }

        [Fact]
        public void ImportMagicItemsFromJson_Items_Should_Be_Cloned_Independently_Like_Open5e_Ones()
        {
            var library = new StandardItemLibrary(_contentSource, _diceRoller);
            library.ImportMagicItemsFromJson(@"{ ""name"": ""Ring of Protection"", ""type"": ""RG"", ""reqAttune"": true }");

            var first = (IMagicItem)library.GetItem("Ring of Protection")!;
            var second = (IMagicItem)library.GetItem("Ring of Protection")!;

            first.Should().NotBeSameAs(second);
        }

        [Fact]
        public void ImportMagicItemsFromJson_Should_Resolve_Attached_Spells_Via_Injected_Repository()
        {
            var spellRepository = Substitute.For<ISpellRepository>();
            spellRepository.GetSpell(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<OpenCombatEngine.Core.Interfaces.Spells.ISpell>.Failure("not found"));
            var library = new StandardItemLibrary(_contentSource, _diceRoller, spellRepository);
            library.ImportMagicItemsFromJson(@"
            {
                ""name"": ""Wand of Fireballs"",
                ""type"": ""WD"",
                ""charges"": 7,
                ""attachedSpells"": [""Fireball""]
            }");

            var item = (IMagicItem)library.GetItem("Wand of Fireballs")!;
            var ability = item.Abilities.Single();

            // Executing the ability should reach into the SAME repository instance passed to
            // the library's constructor, proving the importer was wired up with it rather than
            // some throwaway default.
            ability.Execute(Substitute.For<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>(), Substitute.For<OpenCombatEngine.Core.Interfaces.Actions.IActionContext>());
            spellRepository.Received().GetSpell("Fireball");
        }

        [Fact]
        public void ImportMagicItemsFromJson_Should_Coexist_With_Open5e_Sourced_Items()
        {
            var library = new StandardItemLibrary(_contentSource, _diceRoller);
            library.ImportMagicItemsFromJson(@"{ ""name"": ""Custom Amulet"", ""type"": ""W"" }");

            library.GetAllItems().Should().ContainSingle(i => i.Name == "Custom Amulet");
        }
    }
}
