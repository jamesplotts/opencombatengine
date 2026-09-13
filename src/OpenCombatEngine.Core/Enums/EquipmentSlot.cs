namespace OpenCombatEngine.Core.Enums
{
    public enum EquipmentSlot
    {
        None = 0,
        MainHand,
        OffHand,
        Armor,
        Head,
        Neck,
        Shoulders,
        Hands,
        Waist,
        Feet,
        Ring1,
        Ring2,
        Accessory, // Generic accessory? Or maybe generic "Attuned" slot? Sticking to specific body slots for now.
        Back // A worn backpack/quiver — the slot a carried container is equipped into, distinct from being held.
    }
}
