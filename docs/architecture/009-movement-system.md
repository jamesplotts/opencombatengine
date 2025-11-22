# 009 - Movement System

## Context
In D&D 5e, creatures have a speed (e.g., 30ft) and can move up to that distance during their turn. We need to track how much movement a creature has used or has remaining to enforce this limit.

## Decision
We decided to encapsulate movement logic in an `IMovement` interface composed into `ICreature`.

### Key Components
- **`IMovement`**: Tracks `Speed` (base) and `MovementRemaining` (current turn).
- **`StandardMovement`**: Concrete implementation.
    - `Move(int distance)`: Reduces `MovementRemaining`. Throws if distance > remaining.
    - `ResetTurn()`: Resets `MovementRemaining` to the current `Speed`.
- **Integration**:
    - `ICreature` exposes a `Movement` property.
    - `StandardCreature.StartTurn()` calls `Movement.ResetTurn()`.

## Consequences
- **Pros**:
    - **Enforcement**: Prevents illegal movement distances.
    - **Automation**: Resets automatically at start of turn.
    - **Flexibility**: `Speed` property on `IMovement` delegates to `ICombatStats`, so changes to stats (buffs) are immediately reflected in the *next* reset.
- **Cons**:
    - **Grid Independence**: Currently just tracks abstract "feet". Grid-based logic (diagonals, difficult terrain) would need to be handled by a higher-level system (e.g., a Map Manager) that calculates the "cost" of a move and then calls `Move(cost)`.

## Status
Accepted and Implemented.
