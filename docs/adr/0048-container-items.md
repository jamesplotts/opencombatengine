# 48. Container Items

Date: 2025-11-25

## Status

Accepted

## Context

Players need to organize their inventory using containers (backpacks, sacks, etc.). Some containers, like the *Bag of Holding*, have special rules regarding weight (fixed weight regardless of contents). Previously, the inventory was a flat list of items.

## Decision

We introduced the `IContainer` interface and `ContainerItem` implementation.

1.  **IContainer**: Defines `Contents`, `AddItem`, `RemoveItem`, `WeightCapacity`, and `WeightMultiplier`.
2.  **ContainerItem**: Implements `IContainer`. Calculates weight as `BaseWeight + (ContentsWeight * WeightMultiplier)`.
3.  **MagicItem Integration**: Added `ContainerProperties` to `MagicItem` (composition) to allow magic items to function as containers.
4.  **Item Weight**: Made `Item.Weight` virtual to allow `ContainerItem` to override it for dynamic calculation.

## Consequences

*   **Pros**:
    *   Supports nested inventory.
    *   Supports "magic" containers with fixed weight (multiplier 0.0).
    *   Enforces capacity limits.
*   **Cons**:
    *   Inventory management becomes recursive (finding an item requires searching containers).
    *   Weight calculation is slightly more complex.

## Implementation Details

*   `ContainerItem` logic handles the recursion for weight.
*   `JsonMagicItemImporter` detects "Bag of Holding" and "Handy Haversack" to configure them correctly.
