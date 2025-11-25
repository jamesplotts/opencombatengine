# 46. Magic Items as Equipment

Date: 2025-11-25

## Status

Accepted

## Context

We needed a way for `MagicItem` instances to function as weapons or armor (e.g., a "+1 Longsword" or "Elven Chain") within the `IEquipmentManager`. Previously, `MagicItem` did not implement `IWeapon` or `IArmor`, so they couldn't be equipped in the respective slots or contribute to combat stats.

## Decision

We decided to use **composition** rather than inheritance.

1.  **Interface Updates**: We added `WeaponProperties` (type `IWeapon?`) and `ArmorProperties` (type `IArmor?`) to `IMagicItem`.
2.  **Implementation**: `MagicItem` now holds optional references to `IWeapon` or `IArmor` implementations (specifically `StandardWeapon` and `StandardArmor`).
3.  **Equipment Manager**: We updated `IEquipmentManager` methods (e.g., `EquipMainHand`) to accept `IItem` instead of specific interfaces. The `StandardEquipmentManager` checks if the item is an `IMagicItem` and, if so, extracts the underlying `WeaponProperties` or `ArmorProperties` to use in the slot.

## Consequences

*   **Pros**:
    *   Avoids complex inheritance hierarchies (e.g., `MagicWeapon`, `MagicArmor`, `MagicWondrousItem`).
    *   Allows a single `MagicItem` class to represent any type of item.
    *   Flexible: An item could theoretically have both weapon and armor properties (though rare).
*   **Cons**:
    *   `IEquipmentManager` methods now take `IItem`, requiring runtime type checking/casting within the implementation.
    *   `MagicItemDto` and `JsonMagicItemImporter` had to be updated to parse weapon/armor stats from the generic magic item definition.

## Implementation Details

*   `MagicItemDto` added fields for `dmg1`, `dmgType`, `range`, `ac`, `strength`, `stealth`.
*   `JsonMagicItemImporter` creates `StandardWeapon` or `StandardArmor` instances and passes them to the `MagicItem` constructor.
