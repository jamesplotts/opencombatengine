using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Loot;
using OpenCombatEngine.Implementation.Open5e;
using OpenCombatEngine.Implementation.Open5e.Models;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class LootGenerationTests
    {
        private readonly Open5eClient _mockClient;
        private readonly Open5eContentSource _contentSource;
        private readonly IDiceRoller _mockDice;

        public LootGenerationTests()
        {
            var httpClient = new System.Net.Http.HttpClient();
            _mockClient = Substitute.For<Open5eClient>(httpClient);
            _mockDice = Substitute.For<IDiceRoller>();
            // Default mock dice behavior: returns average-ish or max? 
            // Better to setup specific returns in tests.
            _contentSource = new Open5eContentSource(_mockClient, _mockDice);
        }

        private void SetupMockItems()
        {
            // Mock Weapons
            var weaponList = new Open5eListResult<Open5eWeapon>
            {
                Count = 1
            };
            weaponList.Results.Add(new Open5eWeapon { Name = "Longsword", Slug="longsword", Cost="15 gp", DamageDice="1d8", DamageType="slashing", Weight="3 lb." });
            _mockClient.GetWeaponsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eWeapon>?>(weaponList));
            // Ensure loop breaks
            _mockClient.GetWeaponsAsync(2).Returns(Task.FromResult<Open5eListResult<Open5eWeapon>?>(new Open5eListResult<Open5eWeapon>()));

            // Mock Armor
            var armorList = new Open5eListResult<Open5eArmor>
            {
                Count = 1
            };
            armorList.Results.Add(new Open5eArmor { Name = "Chain Mail", Slug="chain-mail", Category="Heavy Armor", BaseAc=16, Cost="75 gp", Weight="55 lb." });
            _mockClient.GetArmorAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eArmor>?>(armorList));
            _mockClient.GetArmorAsync(2).Returns(Task.FromResult<Open5eListResult<Open5eArmor>?>(new Open5eListResult<Open5eArmor>()));

            // Mock Magic Items
            var magicList = new Open5eListResult<Open5eMagicItem>
            {
                Count = 2
            };
            magicList.Results.Add(new Open5eMagicItem { Name = "Potion of Healing", Slug="potion-healing", Type="Potion", Rarity="Common" });
            magicList.Results.Add(new Open5eMagicItem { Name = "Excalibur", Slug="excalibur", Type="Weapon", Rarity="Legendary" });
            _mockClient.GetMagicItemsAsync(1).Returns(Task.FromResult<Open5eListResult<Open5eMagicItem>?>(magicList));
            _mockClient.GetMagicItemsAsync(2).Returns(Task.FromResult<Open5eListResult<Open5eMagicItem>?>(new Open5eListResult<Open5eMagicItem>()));
        }

        [Fact]
        public async Task Library_Should_Load_Items_From_Source()
        {
            SetupMockItems();
            var library = new StandardItemLibrary(_contentSource, _mockDice);
            await library.InitializeAsync();

            library.GetAllItems().Should().HaveCount(4);
            library.GetWeapon("Longsword").Should().NotBeNull();
            library.GetArmor("Chain Mail").Should().NotBeNull();
            library.GetItem("Potion of Healing").Should().NotBeNull();
        }

        [Fact]
        public async Task Generator_Should_Generate_Tier1_Loot()
        {
            SetupMockItems();
            var library = new StandardItemLibrary(_contentSource, _mockDice);
            await library.InitializeAsync();

            // Force dice rolls for Tier 1
            // 6d6 cp, 3d6 sp, 2d6 gp. Item check 1d100
            // Return max values for easy assertion
            _mockDice.Roll(Arg.Any<string>()).Returns(x => 
            {
                string formula = (string)x[0];
                if (formula == "1d100") return Result<DiceRollResult>.Success(DiceRollResult.Constant(10)); // No item
                return Result<DiceRollResult>.Success(DiceRollResult.Constant(100)); // Lots of gold
            });
            
            var generator = new StandardLootGenerator(library, _mockDice);
            var loot = generator.GenerateLoot(1);

            loot.Copper.Should().BeGreaterThan(0);
            loot.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Generator_Should_Include_Item_On_High_Roll()
        {
            SetupMockItems();
            var library = new StandardItemLibrary(_contentSource, _mockDice);
            await library.InitializeAsync();

             // Comprehensive Mock
            _mockDice.Roll(Arg.Any<string>()).Returns(x => 
            {
                string s = (string)x[0];
                if (s == "1d100") return Result<DiceRollResult>.Success(DiceRollResult.Constant(90)); // Tier 1 Item (Roll > 80)
                if (s.StartsWith("1d")) return Result<DiceRollResult>.Success(DiceRollResult.Constant(1)); // Item Index
                return Result<DiceRollResult>.Success(DiceRollResult.Constant(100)); // Gold
            });

             // Note: StandardItemLibrary needs a specific fix: I mocked 1 common magic item (Potion).
             // Tier 1 drops Common items.
            
            var generator = new StandardLootGenerator(library, _mockDice);
            var loot = generator.GenerateLoot(1);

            loot.Items.Should().HaveCount(1);
            loot.Items[0].Rarity.Should().Be(ItemRarity.Common);
        }
    }
}
