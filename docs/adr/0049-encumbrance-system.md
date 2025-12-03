# 49. Encumbrance System

Date: 2025-11-25

## Status

Accepted

## Context

Characters have limits on the weight they can carry. The system currently tracks item weight but does not enforce limits or apply penalties for overloading.

## Decision

We will implement the **Variant Encumbrance** rules from 5e, as they provide more granular mechanical effects.

1.  **Weight Calculation**:
    *   `IInventory` will expose a `TotalWeight` property, which recursively sums the weight of all items (including container contents).

2.  **Carrying Capacity**:
    *   Defined based on Strength score.
    *   **Unencumbered**: Weight <= Strength * 5.
    *   **Encumbered**: Weight > Strength * 5. Effect: Speed drops by 10 ft.
    *   **Heavily Encumbered**: Weight > Strength * 10. Effect: Speed drops by 20 ft, Disadvantage on Ability Checks (Str, Dex, Con).
    *   **Over Capacity**: Weight > Strength * 15. Effect: Speed drops to 5 ft.

3.  **Implementation**:
    *   `StandardInventory`: Implement recursive `TotalWeight`.
    *   `StandardCreature`:
        *   Add `EncumbranceLevel` property (Enum: None, Encumbered, HeavilyEncumbered, OverCapacity).
        *   Update `GetSpeed()` (or `Movement` component) to apply penalties based on `EncumbranceLevel`.
        *   Update `CheckManager` to apply Disadvantage if Heavily Encumbered.

## Consequences

*   **Pros**:
    *   Adds realism and resource management depth.
    *   Makes Strength more valuable.
*   **Cons**:
    *   Requires recalculation whenever Inventory or Strength changes.
    *   Adds complexity to movement and check resolution.

## Implementation Details

*   We need to ensure `TotalWeight` is cached or calculated efficiently if inventory is large (though usually it's small).
*   `StandardMovement` needs to be aware of Encumbrance.
