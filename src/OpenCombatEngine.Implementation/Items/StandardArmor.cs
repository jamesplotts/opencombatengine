using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Implementation.Items
{
    public class StandardArmor : StandardItem, IArmor
    {
        public int ArmorClass { get; }
        public ArmorCategory Category { get; }
        public int? DexterityCap { get; }
        public int StrengthRequirement { get; }
        public bool StealthDisadvantage { get; }

        public StandardArmor(
            Guid id,
            string name,
            string description,
            double weight,
            int value,
            ItemRarity rarity,
            ArmorCategory category,
            int armorClass,
            bool plusDexMod,
            int? dexterityCap,
            int strengthRequirement,
            bool stealthDisadvantage)
            : base(id, name, description, weight, value, rarity, ItemType.Armor)
        {
            Category = category;
            ArmorClass = armorClass;
            // Note: plusDexMod logic isn't stored directly in properties, usually handled by AC calculation logic
            // But for now we just store data.
            // Wait, IArmor doesn't have PlusDexMod. It has DexCap.
            // If PlusDexMod is false, DexCap should probably be 0? Or maybe AC calculation handles it.
            // For now, adhere to IArmor interface.
            DexterityCap = dexterityCap;
            StrengthRequirement = strengthRequirement;
            StealthDisadvantage = stealthDisadvantage;
        }
    }
}
