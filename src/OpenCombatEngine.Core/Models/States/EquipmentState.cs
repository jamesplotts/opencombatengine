using System.Collections.ObjectModel;
using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a single equipped slot.
    /// </summary>
    /// <param name="Slot">The equipment slot.</param>
    /// <param name="ItemIndex">Index of the equipped item within the parallel <see cref="InventoryState.Items"/> list.</param>
    /// <param name="SlotName">
    /// Purely derived, display-oriented — <paramref name="Slot"/> spaced
    /// out into words ("Main Hand" rather than "MainHand") for a schema-
    /// driven client (Layforge's character sheet) to show directly,
    /// without needing its own copy of this enum's naming convention.
    /// Never independently stored; recomputed fresh every time, same
    /// "engine computes, client just displays" reasoning as
    /// <c>AbilityEntry</c>/<c>SkillEntry</c>. Null only for state saved
    /// before this field existed.
    /// </param>
    /// <param name="ItemName">
    /// Purely derived: the equipped item's own name, resolved from
    /// <paramref name="ItemIndex"/> at the moment this state was built —
    /// spares a schema-driven client from cross-referencing
    /// <see cref="InventoryState.Items"/> by index itself, which its own
    /// generic rendering has no domain knowledge to do. Null only for
    /// state saved before this field existed.
    /// </param>
    public record EquippedSlotState(EquipmentSlot Slot, int ItemIndex, string? SlotName = null, string? ItemName = null);

    /// <summary>
    /// Serializable state for an equipment manager component.
    /// </summary>
    /// <param name="EquippedSlots">Which inventory items (by index) occupy which slots.</param>
    /// <param name="AttunedItemIndices">Indices, within the parallel <see cref="InventoryState.Items"/> list, of currently attuned magic items.</param>
    /// <param name="AttunedItemNames">
    /// Purely derived: <paramref name="AttunedItemIndices"/> resolved to
    /// each item's own name, in the same order — same reasoning as
    /// <see cref="EquippedSlotState.ItemName"/>. Null only for state
    /// saved before this field existed.
    /// </param>
    public record EquipmentState(
        Collection<EquippedSlotState> EquippedSlots,
        Collection<int> AttunedItemIndices,
        Collection<string>? AttunedItemNames = null);
}
