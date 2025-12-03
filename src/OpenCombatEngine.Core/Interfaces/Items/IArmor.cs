using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public enum ArmorCategory
    {
        Light,
        Medium,
        Heavy,
        Shield
    }

    public interface IArmor : IItem
    {
        int ArmorClass { get; }
        ArmorCategory Category { get; }
        int? DexterityCap { get; }
        int StrengthRequirement { get; }
        bool StealthDisadvantage { get; }
    }
}
