using System.Collections.ObjectModel;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a single inventory item instance.
    /// </summary>
    /// <param name="Name">The item's name, used to resolve the base item via an item library on restore.</param>
    /// <param name="CurrentCharges">Current charges remaining, for magic items that track charges.</param>
    /// <param name="Contents">Nested items, for items that are containers.</param>
    public record ItemInstanceState(string Name, int? CurrentCharges = null, Collection<ItemInstanceState>? Contents = null);

    /// <summary>
    /// Serializable state for an inventory component.
    /// </summary>
    /// <param name="Items">Items in the inventory, in order.</param>
    /// <param name="Copper">Copper pieces carried.</param>
    /// <param name="Silver">Silver pieces carried.</param>
    /// <param name="Gold">Gold pieces carried.</param>
    /// <param name="Platinum">Platinum pieces carried.</param>
    public record InventoryState(
        Collection<ItemInstanceState> Items,
        int Copper = 0,
        int Silver = 0,
        int Gold = 0,
        int Platinum = 0);
}
