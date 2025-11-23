# ADR 013: Combat Actions Integration

## Status
Accepted

## Context
The system defined `IAction` but lacked concrete implementations for standard combat actions like Move and Attack.

## Decision
We implemented `MoveAction` and `AttackAction`.
- **MoveAction**: Consumes movement from `IMovement`.
- **AttackAction**: Performs an attack roll using `IDiceRoller` and returns an `AttackResult`.
- **Integration**: Actions are returned by `ICreature.GetActions()` (later refactored, but initially this was the pattern).

## Consequences
- **Positive**: Provides the basic verbs of combat.
- **Negative**: `AttackAction` logic is complex and was later refactored to use `AttackResult` pipeline more robustly.
