using System.Collections.ObjectModel;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a single inventory item instance.
    /// </summary>
    /// <param name="Name">The item's name, used to resolve the base item via an item library on restore.</param>
    /// <param name="CurrentCharges">Current charges remaining, for magic items that track charges.</param>
    public record ItemInstanceState(string Name, int? CurrentCharges = null);

    /// <summary>
    /// Serializable state for an inventory component.
    /// </summary>
    /// <param name="Items">Items in the inventory, in order.</param>
    public record InventoryState(Collection<ItemInstanceState> Items);
}
