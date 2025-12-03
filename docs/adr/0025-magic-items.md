# ADR 025: Magic Items System

## Status
Accepted

## Context
The system currently supports basic items (Weapons, Armor) but lacks support for magic items that provide passive bonuses (e.g., +1 AC) or require attunement (limiting powerful items).

## Decision
We will introduce an `IMagicItem` interface that extends `IItem`.
Key features:
- **Attunement**: Items can require attunement. A creature can be attuned to a maximum of 3 items at once.
- **Passive Bonuses**: Magic items will leverage the existing `IFeature` or `ICondition` system to apply bonuses when equipped/attuned.
- **Management**: `IEquipmentManager` will be updated to track attuned items and enforce limits.

## Consequences
- **Positive**: Enables a wide range of magic items (Rings of Protection, Magic Weapons).
- **Negative**: Adds complexity to `IEquipmentManager`.
- **Future**: We will need to implement specific magic items using this system.
