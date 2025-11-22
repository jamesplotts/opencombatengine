using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Implementation.Items
{
    public class Armor : Item, IArmor
    {
        public int ArmorClass { get; }
        public ArmorCategory Category { get; }
        public int? DexterityCap { get; }
        public int StrengthRequirement { get; }
        public bool StealthDisadvantage { get; }

        public Armor(string name, int armorClass, ArmorCategory category, int? dexterityCap = null, int strengthRequirement = 0, bool stealthDisadvantage = false, string description = "", double weight = 0, int value = 0)
            : base(name, description, weight, value)
        {
            ArmorClass = armorClass;
            Category = category;
            DexterityCap = dexterityCap;
            StrengthRequirement = strengthRequirement;
            StealthDisadvantage = stealthDisadvantage;
        }
    }
}
