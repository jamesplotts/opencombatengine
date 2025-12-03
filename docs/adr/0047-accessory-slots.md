# 47. Accessory Slots

Date: 2025-11-25

## Status

Accepted

## Context

The system previously only supported `MainHand`, `OffHand`, `Armor`, and `Shield` slots. Users need to equip accessories like boots, cloaks, rings, and amulets, which are common in RPGs and often provide magical benefits.

## Decision

We added specific slots to the `IEquipmentManager` interface and `StandardEquipmentManager` implementation:
*   `Head`
*   `Neck`
*   `Shoulders`
*   `Hands`
*   `Waist`
*   `Feet`
*   `Ring1`
*   `Ring2`

We also introduced an `EquipmentSlot` enum to strongly type these slots and allow for generic `Equip(item, slot)` methods.

## Consequences

*   **Pros**:
    *   Allows for proper slot management (e.g., preventing wearing two pairs of boots).
    *   Aligns with standard RPG mechanics (5e).
    *   `MagicItem` can now hint at its default slot via `DefaultSlot` property.
*   **Cons**:
    *   Increases the surface area of `IEquipmentManager`.
    *   UI will need to handle these additional slots.

## Implementation Details

*   `MagicItem` has a new `DefaultSlot` property.
*   `JsonMagicItemImporter` infers the default slot from the item name (e.g., "boots" -> `Feet`).
*   `StandardEquipmentManager` implements storage for these slots and validates equipping logic.
