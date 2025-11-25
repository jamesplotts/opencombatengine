using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IEquipmentManager
    {
        IWeapon? MainHand { get; }
        IWeapon? OffHand { get; }
        IArmor? Armor { get; }
        IArmor? Shield { get; }

        Result<bool> EquipMainHand(IItem item);
        Result<bool> EquipOffHand(IItem item);
        Result<bool> EquipArmor(IItem item);
        Result<bool> EquipShield(IItem item);

        void UnequipMainHand();
        void UnequipOffHand();
        void UnequipArmor();
        void UnequipShield();

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
    }
}
