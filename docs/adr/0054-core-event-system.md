# 54. Core Event System

Date: 2025-12-06

## Status

Accepted

## Context

As the engine grows, subsystems need to react to changes in state without tight coupling or polling. For example:
- The UI needs to know when a creature moves to animate it.
- The Reaction System needs to know when a creature moves to trigger opportunity attacks.
- Conditions need to know when a turn starts or ends.
- Features might trigger on specific actions (e.g., "Sentinel" feat reacting to attacks).

Previously, we used direct method calls (e.g., `GridManager` calling `NotifyMoved`) or tightly coupled logic. We need a standardized event pattern.

## Decision

We will implement a Core Event System using standard C# `event` patterns (`EventHandler<T>`).

### 1. Event Arguments
We define strongly-typed event arguments in `OpenCombatEngine.Core.Models.Events`:
- `MovedEventArgs`: Contains `From` and `To` positions.
- `ConditionEventArgs`: Contains the `ICondition` added or removed.
- `ActionEventArgs`: Contains the `ActionName` and `Target`.
- `TurnChangedEventArgs`: (Already existed) Contains `Creature` and `Round`.

### 2. Interface Enhancements
We update core interfaces to expose these events:
- `IMovement`: Adds `event EventHandler<MovedEventArgs> Moved`.
- `IConditionManager`: Adds `ConditionAdded`, `ConditionRemoved`.
- `ICreature`: Adds `ActionStarted`, `ActionEnded`.

### 3. Implementation Details
- **StandardMovement**: Implements the `Moved` event. It exposes a `NotifyMoved` method so the `GridManager` (which controls *logic* of movement) can trigger the event on the *data* component (`Movement`).
- **StandardConditionManager**: Triggers events when `_conditions` list is modified.
- **StandardCreature**: Triggers action events in `PerformAction`, wrapping the `Evaluate`/`Execute` logic.

### 4. GridManager Integration
The `IGridManager` is the authority on positioning. When `MoveCreature` succeeds, it calls `NotifyMoved` on the creature's `IMovement` component, ensuring the event fires exactly when the state changes.

## Consequences

### Positive
- **Decoupling**: UI and other systems can subscribe to events without modifying the core logic.
- **Observability**: Tests can verify state changes by listening to events.
- **Extensibility**: New features (like Reactions) can be built purely by listening to these events.

### Negative
- **Complexity**: Managing event subscriptions (memory leaks if not unsubscribed) becomes a concern.
- **Debugging**: Flow of control is less linear ("Action at a distance").

## Compliance
- All new events follow standard .NET `EventHandler<T>` pattern.
- Arguments are immutable (records or read-only properties).
