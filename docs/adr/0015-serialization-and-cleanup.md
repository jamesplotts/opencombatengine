# ADR 015: Serialization and Cleanup

## Status
Accepted

## Context
As new components (CombatStats, Conditions) were added, the serialization system (`CreatureState`) became outdated.

## Decision
We updated `CreatureState` to include state objects for all new components.
- **State Objects**: Added `CombatStatsState`, `ConditionManagerState`.
- **Integration**: Updated `StandardCreature` constructor to restore these states.

## Consequences
- **Positive**: Ensures full creature persistence.
- **Negative**: Serialization DTOs must be kept in sync with component evolution.
