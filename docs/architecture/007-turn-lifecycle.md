# 007 - Turn Lifecycle Integration

## Context
We have a **Turn Management** system that cycles through creatures, and a **Conditions System** that tracks duration-based effects. However, these two systems were disconnected; conditions were not automatically expiring because the `Tick()` method was never called. We needed a mechanism to trigger start-of-turn logic.

## Decision
We decided to add explicit lifecycle methods to the `ICreature` interface and integrate them into the `TurnManager`.

### Key Changes
- **`ICreature.StartTurn()`**: A new method called at the start of a creature's turn.
    - In `StandardCreature`, this calls `Conditions.Tick()`.
- **`ICreature.EndTurn()`**: A new method called at the end of a creature's turn (currently a placeholder for future cleanup).
- **`StandardTurnManager` Integration**:
    - The `NextTurn()` method now calls `StartTurn()` on the new `CurrentCreature`.
    - `StartCombat()` was updated to automatically trigger the first turn (calling `NextTurn()` internally instead of just setting indices), ensuring the first creature gets their start-of-turn effects processed.

## Consequences
- **Pros**:
    - **Automation**: Buffs and debuffs now expire automatically without manual intervention.
    - **Extensibility**: `StartTurn` provides a hook for other future features (e.g., regeneration, recharging abilities).
    - **Correctness**: Ensures the game state is consistent with 5e rules (effects happen at start of turn).
- **Cons**:
    - **Coupling**: `TurnManager` now requires `ICreature` to have these methods, slightly increasing coupling, but this is necessary for the domain.

## Status
Accepted and Implemented.
