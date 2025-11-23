# ADR 030: Action Context

## Status
Accepted

## Context
Actions need access to more than just the source and target creature to function in a grid-based system. Specifically, `MoveAction` needs access to the `IGridManager` to validate paths and update positions. Additionally, actions may target positions (movement, AOE) rather than specific creatures.

## Decision
We will refactor `IAction.Execute` to accept a single `IActionContext` argument.
- **IActionContext**: Provides access to `Source`, `Target`, `Grid`, and other contextual services.
- **IActionTarget**: An abstraction for action targets, allowing for `CreatureTarget`, `PositionTarget`, etc.

## Consequences
- **Positive**: Enables context-aware actions (Grid, Environment). Flexible targeting.
- **Negative**: Breaking change for `IAction`. Requires updating all existing actions and tests.
