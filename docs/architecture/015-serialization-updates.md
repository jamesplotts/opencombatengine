# 15. Serialization Updates

Date: 2025-11-22

## Status

Accepted

## Context

As new systems (Conditions, CombatStats, Inventory) were added, the serialization model (`CreatureState`) needed to evolve to persist this data.

## Decision

We updated the State DTOs and `ToState`/`FromState` methods.

- **CreatureState**: Added `Conditions` (list of strings/enums), `Inventory` (list of item states), and updated `HitPoints` (death saves).
- **JSON Serialization**: Validated that complex nested objects (like a Creature holding Items) serialize correctly to JSON.
- **Refactoring**: Cleaned up `StandardCreature` serialization logic to delegate to sub-components (`HitPoints.GetState()`, `Inventory.GetState()`).

## Consequences

- Full creature state can be saved and loaded.
- Backward compatibility is not guaranteed during this rapid development phase (breaking changes to schema are accepted).
- `System.Text.Json` is the chosen serializer.
