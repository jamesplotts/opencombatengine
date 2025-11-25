using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    /// <summary>
    /// Represents a magic item that may require attunement.
    /// </summary>
    public interface IMagicItem : IItem
    {
        /// <summary>
        /// Gets whether this item requires attunement to function.
        /// </summary>
        bool RequiresAttunement { get; }

        ItemType Type { get; }

        /// <summary>
        /// Gets the creature currently attuned to this item, if any.
        /// </summary>
        ICreature? AttunedCreature { get; }

        /// <summary>
        /// Attunes the item to the specified creature.
        /// </summary>
        /// <param name="creature">The creature to attune to.</param>
        /// <returns>Result of the attunement.</returns>
        Result<bool> Attune(ICreature creature);

        /// <summary>
        /// Ends the attunement to the item.
        /// </summary>
        /// <returns>Result of the operation.</returns>
        Result<bool> Unattune();

        /// <summary>
        /// Gets the features granted by this item.
        /// </summary>
        System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Features.IFeature> Features { get; }

        /// <summary>
        /// Gets the current number of charges.
        /// </summary>
        int Charges { get; }

        /// <summary>
        /// Gets the maximum number of charges.
        /// </summary>
        int MaxCharges { get; }

        /// <summary>
        /// Gets the recharge rate description (e.g. "1d6+1 at dawn").
        /// </summary>
        string RechargeRate { get; }

        /// <summary>
        /// Consumes the specified number of charges.
        /// </summary>
        /// <param name="amount">Amount to consume.</param>
        /// <returns>Success if enough charges available.</returns>
        Result<int> ConsumeCharges(int amount);

        /// <summary>
        /// Recharges the item by the specified amount.
        /// </summary>
        /// <param name="amount">Amount to add.</param>
        /// <returns>New charge count.</returns>
        Result<int> Recharge(int amount);

        /// <summary>
        /// Gets the conditions applied by this item.
        /// </summary>
        System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Conditions.ICondition> Conditions { get; }

        /// <summary>
        /// Gets the weapon properties if this item is a weapon.
        /// </summary>
        IWeapon? WeaponProperties { get; }

        /// <summary>
        /// Gets the armor properties if this item is armor.
        /// </summary>
        IArmor? ArmorProperties { get; }
    }
}
