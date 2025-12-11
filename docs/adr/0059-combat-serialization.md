# 59. Combat Serialization Strategy

Date: 2025-12-11

## Status

Accepted

## Context

To support saving and loading games, the engine required a way to persist the state of a combat encounter. This includes:
-   The list of participants (Creatures) and their dynamic state (HP, Position, Conditions).
-   The state of the `TurnManager` (Current Round, Initiative Order, Current Actor).
-   The active `WinCondition`.

Restoring this state is complex because of object references (e.g., the `TurnManager` holds a list of `ICreature` references that must point to the specific restored instances).

## Decision

We decided to implement a **Memento-like pattern** using DTOs (Data Transfer Objects) and an `IStateful<T>` interface.

1.  **State DTOs**: We created purely data-holding records (`CombatState`, `TurnManagerState`, `CreatureState`) in `OpenCombatEngine.Core.Models.States`. These are decoupled from logic.
2.  **`IStateful<T>` Interface**: Components that need persistence implement `IStateful<T>` (where `T` is their state DTO). They provide `GetState()` and `RestoreState(T)` methods.
3.  **JSON Serialization**: We use `System.Text.Json` in a `CombatSerializer` service to handle the conversion of the root `CombatState` to/from strings.
4.  **Restoration Logic**:
    -   `StandardCombatManager` orchestrates the restoration.
    -   It first restores all Participants.
    -   It then passes the restored Participant list to `StandardTurnManager.RestoreState`, allowing the Turn Manager to rebuild its internal initiative order using matching IDs.
    -   Events (like `Died`) are re-subscribed during restoration to ensure game logic (Win Conditions) continues to function.

## Consequences

### Positive
-   **Separation of Concerns**: Logic stays in classes, data stays in DTOs.
-   **Flexibility**: JSON format is human-readable and version-tolerant.
-   **Testability**: `CombatSerializationTests` verify the fidelity of the save/load process.

### Negative
-   **Boilerplate**: Every stateful component requires a corresponding DTO and mapping logic.
-   **Reference Complexity**: Reconnecting references (like Turn Order) requires careful ordering during restoration.

## Compliance
-   Verified by `CombatSerializationTests` and `EndToEndCombatTests`.
