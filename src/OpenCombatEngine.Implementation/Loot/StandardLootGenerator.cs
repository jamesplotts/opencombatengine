using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Loot;

namespace OpenCombatEngine.Implementation.Loot
{
    public class StandardLootGenerator : ILootGenerator
    {
        private readonly IItemLibrary _itemLibrary;
        private readonly IDiceRoller _diceRoller;

        public StandardLootGenerator(IItemLibrary itemLibrary, IDiceRoller diceRoller)
        {
            _itemLibrary = itemLibrary ?? throw new ArgumentNullException(nameof(itemLibrary));
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
        }

        public LootBundle GenerateLoot(int challengeRating)
        {
            var bundle = new LootBundle();

            if (challengeRating < 0) challengeRating = 0;

            // Simple Tier Logic
            // Tier 1: CR 0-4
            // Tier 2: CR 5-10
            // Tier 3: CR 11-16
            // Tier 4: CR 17+

            if (challengeRating <= 4)
            {
                GenerateTier1Loot(bundle);
            }
            else if (challengeRating <= 10)
            {
                GenerateTier2Loot(bundle);
            }
            else if (challengeRating <= 16)
            {
                GenerateTier3Loot(bundle);
            }
            else
            {
                GenerateTier4Loot(bundle);
            }

            return bundle;
        }

        private void GenerateTier1Loot(LootBundle bundle)
        {
            // Coins
            bundle.Copper = Roll("6d6") * 100;
            bundle.Silver = Roll("3d6") * 100;
            bundle.Gold = Roll("2d6") * 10;

            // Items? Small chance.
            if (Roll("1d100") > 80) // 20% chance
            {
                 var item = _itemLibrary.GetRandomItem(ItemRarity.Common, null); // Any type
                 if (item != null) bundle.Items.Add(item);
            }
        }

        private void GenerateTier2Loot(LootBundle bundle)
        {
            bundle.Silver = Roll("6d6") * 100;
            bundle.Gold = Roll("2d6") * 100;
            bundle.Platinum = Roll("1d6") * 10;

            if (Roll("1d100") > 60) // 40% chance
            {
                // Uncommon or Rare
                var rarity = Roll("1d2") == 1 ? ItemRarity.Uncommon : ItemRarity.Rare;
                var item = _itemLibrary.GetRandomItem(rarity, null);
                if (item != null) bundle.Items.Add(item);
            }
        }

        private void GenerateTier3Loot(LootBundle bundle)
        {
            bundle.Gold = Roll("4d6") * 1000;
            bundle.Platinum = Roll("5d6") * 100;

            if (Roll("1d100") > 40) // 60% chance
            {
                var rarity = Roll("1d2") == 1 ? ItemRarity.Rare : ItemRarity.VeryRare;
                var item = _itemLibrary.GetRandomItem(rarity, null);
                if (item != null) bundle.Items.Add(item);
            }
        }

        private void GenerateTier4Loot(LootBundle bundle)
        {
            bundle.Gold = Roll("12d6") * 1000;
            bundle.Platinum = Roll("8d6") * 1000;

            // Guaranteed Item
            var roll = Roll("1d100");
            ItemRarity rarity;
            if (roll > 90) rarity = ItemRarity.Legendary;
            else if (roll > 50) rarity = ItemRarity.VeryRare;
            else rarity = ItemRarity.Rare;

            var item = _itemLibrary.GetRandomItem(rarity, null);
            if (item != null) bundle.Items.Add(item);
        }

        private int Roll(string formula)
        {
            var result = _diceRoller.Roll(formula);
            return result.IsSuccess ? result.Value.Total : 0;
        }
    }
}
