# 005 - Combat Events (Standard C# Events)

## Context
The engine needs a way to broadcast state changes (e.g., turn progression, damage taken) to external systems like the UI or a Combat Log, as well as to internal game logic (e.g., regeneration effects).

## Decision
We decided to use standard C# **Events** (`EventHandler<T>`) exposed on the core interfaces.

### Key Interfaces
- **`ITurnManager`**: Exposes `TurnChanged`, `RoundChanged`, and `CombatEnded`.
- **`IHitPoints`**: Exposes `DamageTaken`, `Healed`, and `Died`.

### Implementation
- **Event Arguments**: Defined strongly-typed arguments (e.g., `TurnChangedEventArgs`, `DamageTakenEventArgs`) to pass relevant context.
- **Standard Components**: `StandardTurnManager` and `StandardHitPoints` implement the triggering logic.

## Consequences
- **Pros**:
    - **Idiomatic**: Uses standard .NET patterns familiar to developers.
    - **Type-Safe**: Event arguments ensure consumers get the correct data.
    - **Decoupled**: Consumers subscribe to interfaces, not concrete implementations.
- **Cons**:
    - **Memory Management**: Subscribers must unsubscribe to avoid memory leaks (standard event caveat).

## Status
Accepted and Implemented.
