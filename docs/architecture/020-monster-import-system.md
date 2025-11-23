# 20. Monster Import System

Date: 2025-11-22

## Status

Accepted

## Context

We need to import monsters from external data sources (5eTools JSON) to populate the game world. Monsters differ from player characters in that their actions are often ad-hoc and defined in their stat block (e.g., "Bite", "Claw") rather than derived from equipped weapons.

## Decision

We implemented a `JsonMonsterImporter` and a `MonsterAttackAction`.

- **Importer**: Parses 5eTools monster JSONs.
    - Maps Ability Scores, HP, AC (basic), and Speed.
    - Parses "action" array into `MonsterAttackAction` instances.
- **MonsterAttackAction**: A data-driven `IAction` implementation.
    - Stores `ToHitBonus`, `DamageDice`, and `DamageType` directly.
    - `Execute` rolls d20 + Bonus and parses the damage dice string (e.g., "1d6+2").
- **StandardCreature Updates**:
    - Added `AddAction(IAction)` to allow attaching these imported actions.
    - Updated `GetActions()` to return these custom actions alongside standard ones.

## Consequences

- We can now instantiate fully functional monsters from JSON.
- `StandardCreature` is more flexible, supporting arbitrary actions.
- `MonsterAttackAction` provides a pattern for other data-driven actions (e.g., Lair Actions).
- Parsing logic in `MonsterAttackAction` is currently simple and may need to be robustified for complex dice expressions.
