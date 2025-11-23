using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IEquipmentManager
    {
        IWeapon? MainHand { get; }
        IWeapon? OffHand { get; }
        IArmor? Armor { get; }
        IArmor? Shield { get; }

        Result<bool> EquipMainHand(IWeapon weapon);
        Result<bool> EquipOffHand(IWeapon weapon);
        Result<bool> EquipArmor(IArmor armor);
        Result<bool> EquipShield(IArmor shield);

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
