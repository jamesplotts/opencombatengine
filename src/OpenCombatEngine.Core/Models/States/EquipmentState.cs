using System.Collections.ObjectModel;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a single equipped slot.
    /// </summary>
    /// <param name="Slot">The equipment slot.</param>
    /// <param name="ItemIndex">Index of the equipped item within the parallel <see cref="InventoryState.Items"/> list.</param>
    public record EquippedSlotState(EquipmentSlot Slot, int ItemIndex);

    /// <summary>
    /// Serializable state for an equipment manager component.
    /// </summary>
    /// <param name="EquippedSlots">Which inventory items (by index) occupy which slots.</param>
    /// <param name="AttunedItemIndices">Indices, within the parallel <see cref="InventoryState.Items"/> list, of currently attuned magic items.</param>
    public record EquipmentState(
        Collection<EquippedSlotState> EquippedSlots,
        Collection<int> AttunedItemIndices);
}
