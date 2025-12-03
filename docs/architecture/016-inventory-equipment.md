# 16. Inventory & Equipment

Date: 2025-11-22

## Status

Accepted

## Context

Creatures need to carry items and equip weapons/armor to influence combat stats (AC, Damage).

## Decision

We implemented `IInventory` and `IEquipmentManager`.

- **IItem**: Base interface for all items (Name, Weight, Value).
- **IWeapon**: Extends `IItem` with Damage Dice and Damage Type.
- **IArmor**: Extends `IItem` with AC Bonus and Armor Type (Light, Medium, Heavy).
- **IInventory**: Manages a list of `IItem`.
- **IEquipmentManager**: Manages equipped items (MainHand, OffHand, Armor).
- **Integration**:
    - `ICombatStats.ArmorClass` calculates AC based on equipped Armor + Dex Mod.
    - `AttackAction` uses equipped Weapon for damage rolls.

## Consequences

- Combat stats are now dynamic based on equipment.
- Inventory management allows for loot and resource tracking.
- Equipment slots are currently simplified (Main/Off/Armor).
