# ADR 016: Inventory and Equipment

## Status
Accepted

## Context
Creatures need to possess and use items, weapons, and armor.

## Decision
We implemented a split between Inventory (possession) and Equipment (usage).
- **Interfaces**: `IItem`, `IWeapon`, `IArmor`.
- **Inventory**: `StandardInventory` stores a list of items.
- **Equipment**: `StandardEquipmentManager` handles equipping items to slots (MainHand, OffHand, Armor).
- **Integration**: `StandardCombatStats` calculates AC based on equipped armor.

## Consequences
- **Positive**: Clear separation of concerns. Allows for complex equipment logic (e.g. attunement later).
- **Negative**: Synchronization between inventory and equipment (can you equip something you don't have?) needs to be managed (currently loose coupling).
