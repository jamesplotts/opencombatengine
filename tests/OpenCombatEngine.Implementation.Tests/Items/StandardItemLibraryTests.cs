using System.Collections.Generic;
using System.IO;
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
using OpenCombatEngine.Implementation.Open5e.Models;
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

        [Fact]
        public void ImportMagicItemsFromFile_Should_Read_And_Import_Same_As_FromJson()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, @"{ ""name"": ""Boots of Speed"", ""type"": ""W"", ""reqAttune"": true }");

                var library = new StandardItemLibrary(_contentSource, _diceRoller);
                var result = library.ImportMagicItemsFromFile(path);

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().ContainSingle(i => i.Name == "Boots of Speed");
                library.GetItem("Boots of Speed").Should().NotBeNull();
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ImportMagicItemsFromFile_Should_Fail_Gracefully_If_File_Missing()
        {
            var library = new StandardItemLibrary(_contentSource, _diceRoller);

            var result = library.ImportMagicItemsFromFile(Path.Combine(Path.GetTempPath(), "does-not-exist-" + System.Guid.NewGuid() + ".json"));

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void ImportMagicItemsFromFile_Should_Fail_Gracefully_If_Path_Empty()
        {
            var library = new StandardItemLibrary(_contentSource, _diceRoller);

            var result = library.ImportMagicItemsFromFile(string.Empty);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void InitializeFromDtos_Should_Map_And_Add_All_Three_Item_Types()
        {
            var library = new StandardItemLibrary(_contentSource, _diceRoller);
            var weapons = new List<Open5eWeapon> { new() { Name = "Longsword", Slug = "longsword", DamageDice = "1d8", DamageType = "slashing" } };
            var armor = new List<Open5eArmor> { new() { Name = "Chain Mail", Slug = "chain-mail", BaseAc = 16 } };
            var magicItems = new List<Open5eMagicItem> { new() { Name = "Potion of Healing", Slug = "potion-of-healing", Rarity = "Common" } };

            library.InitializeFromDtos(weapons, armor, magicItems);

            library.GetAllItems().Should().HaveCount(3);
            library.GetWeapon("Longsword").Should().NotBeNull();
            library.GetArmor("Chain Mail").Should().NotBeNull();
            library.GetItem("Potion of Healing").Should().NotBeNull();
        }

        [Fact]
        public void InitializeFromDtos_CalledTwice_Should_Not_Duplicate_Items()
        {
            // Mirrors InitializeAsync's own idempotency guard (_isInitialized) -
            // a cache-hit startup path must be safe to call exactly like the
            // live-fetch path already is.
            var library = new StandardItemLibrary(_contentSource, _diceRoller);
            var weapons = new List<Open5eWeapon> { new() { Name = "Longsword", Slug = "longsword" } };

            library.InitializeFromDtos(weapons, new List<Open5eArmor>(), new List<Open5eMagicItem>());
            library.InitializeFromDtos(weapons, new List<Open5eArmor>(), new List<Open5eMagicItem>());

            library.GetAllItems().Should().ContainSingle();
        }
    }
}
