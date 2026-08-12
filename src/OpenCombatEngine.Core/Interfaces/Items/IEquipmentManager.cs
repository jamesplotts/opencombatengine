using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IEquipmentManager
    {
        IWeapon? MainHand { get; }
        IWeapon? OffHand { get; }
        IArmor? Armor { get; }
        IArmor? Shield { get; }
        
        IItem? Head { get; }
        IItem? Neck { get; }
        IItem? Shoulders { get; }
        IItem? Hands { get; }
        IItem? Waist { get; }
        IItem? Feet { get; }
        IItem? Ring1 { get; }
        IItem? Ring2 { get; }

        Result<bool> EquipMainHand(IItem item);
        Result<bool> EquipOffHand(IItem item);
        Result<bool> EquipArmor(IItem item);
        Result<bool> EquipShield(IItem item);
        
        Result<bool> Equip(IItem item, OpenCombatEngine.Core.Enums.EquipmentSlot slot);
        Result<bool> Unequip(OpenCombatEngine.Core.Enums.EquipmentSlot slot);

        void UnequipMainHand();
        void UnequipOffHand();
        void UnequipArmor();
        void UnequipShield();

        /// <summary>
        /// Unequips the given item from whichever slot(s) it currently occupies, if any.
        /// </summary>
        /// <param name="item">The item to unequip.</param>
        /// <returns>Success if the item was found and unequipped; failure if it was not equipped.</returns>
        Result<bool> UnequipItem(IItem item);

        /// <summary>
        /// Gets the list of currently attuned magic items.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<IMagicItem> AttunedItems { get; }

        /// <summary>
        /// Attempts to attune to the specified magic item.
        /// </summary>
        /// <param name="item">The item to attune to.</param>
        /// <returns>Result of the attunement.</returns>
        Result<bool> AttuneItem(IMagicItem item);

        /// <summary>
        /// Ends attunement to the specified magic item.
        /// </summary>
        /// <param name="item">The item to unattune from.</param>
        /// <returns>Result of the operation.</returns>
        Result<bool> UnattuneItem(IMagicItem item);

        /// <summary>
        /// Gets all equipped items.
        /// </summary>
        System.Collections.Generic.IEnumerable<IItem> GetEquippedItems();
    }
}
