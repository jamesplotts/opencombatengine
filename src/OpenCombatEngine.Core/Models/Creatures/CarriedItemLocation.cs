// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Items;

namespace OpenCombatEngine.Core.Models.Creatures
{
    /// <summary>
    /// Where one of a creature's carried items currently is — computed on
    /// demand (see <see cref="Interfaces.Creatures.ICreature.GetCarriedItemLocations"/>),
    /// never stored: an item's location is always derivable from real,
    /// already-persisted structure (which <see cref="EquipmentSlot"/> it
    /// occupies, or which <see cref="IContainer"/>'s Contents it's nested
    /// in), so there is nothing here that needs its own wiring through a
    /// save/restore path to avoid silently going stale.
    /// </summary>
    /// <param name="Item">The item itself.</param>
    /// <param name="ParentContainer">
    /// The container this item is nested inside (<c>Contents</c>), or null
    /// if it isn't stowed in anything. Mutually exclusive with
    /// <paramref name="EquippedSlot"/> in practice — an equipped item is
    /// never also inside a container — but both null means "quick access":
    /// loose in the creature's flat inventory, not equipped, not stowed.
    /// </param>
    /// <param name="EquippedSlot">
    /// The equipment slot this item currently occupies, or null if it
    /// isn't equipped.
    /// </param>
    public sealed record CarriedItemLocation(IItem Item, IContainer? ParentContainer, EquipmentSlot? EquippedSlot)
    {
        /// <summary>
        /// True when the item is neither equipped nor stowed in a
        /// container — carried loose, ready to use with at most a free
        /// object interaction (see <see cref="Interfaces.Creatures.IActionEconomy.TryUseFreeObjectInteraction"/>).
        /// </summary>
        public bool IsQuickAccess => ParentContainer is null && EquippedSlot is null;
    }
}
