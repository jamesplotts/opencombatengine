# 13. Combat Actions Integration

Date: 2025-11-22

## Status

Accepted

## Context

Creatures need to perform actions in combat (Attack, Move, Dash, etc.). We needed to integrate the `IAction` system with the `IActionEconomy` system.

## Decision

We implemented concrete actions and integrated them with the Action Economy.

- **MoveAction**: Consumes `Movement` resource.
- **AttackAction**: Consumes `Action` resource (Standard Action).
- **Execution Flow**:
    1. `IAction.Execute(actor, target)` called.
    2. Action checks `IActionEconomy` (e.g. `HasAction`, `HasMovement`).
    3. If resources available, Action performs logic (e.g. `RollAttack`, `TakeDamage`).
    4. Action consumes resource via `IActionEconomy.UseAction()` or `IMovement.UseMovement()`.
    5. Returns `ActionResult`.

## Consequences

- Actions are now resource-constrained.
- `IActionEconomy` acts as the gatekeeper for what a creature can do in a turn.
- Separation of concerns: Action defines *what* happens, Economy defines *if* it can happen.
