# 006 - Conditions System (Buffs/Debuffs)

## Context
Creatures in combat often suffer from status effects (Conditions) like "Blinded", "Charmed", or "Prone", or benefit from buffs like "Bless". We need a standardized way to apply, track, and expire these effects.

## Decision
We decided to implement a **Condition Manager** component on the `ICreature` interface.

### Key Interfaces
- **`ICondition`**: Defines a condition's metadata (Name, Description) and behavior (Duration, OnApplied, OnTurnStart).
- **`IConditionManager`**: Manages the list of active conditions on a creature and handles the "Tick" logic to decrement durations.

### Implementation
- **`StandardConditionManager`**:
    - Stores active conditions.
    - `Tick()`: Iterates through conditions, triggers `OnTurnStart`, decrements duration, and removes expired conditions (Duration == 0).
- **`Condition`**: A base class for creating concrete conditions.

## Consequences
- **Pros**:
    - **Extensible**: New conditions can be created by implementing `ICondition` or inheriting from `Condition`.
    - **Centralized**: All status effects are managed in one place (`Conditions` property).
    - **Automated**: Duration tracking is handled automatically by the manager's `Tick()` method (which is called by the Turn Manager).
- **Cons**:
    - **Complexity**: Interactions between conditions (e.g., "Immunity to Poison") need to be handled carefully, likely within the `OnApplied` logic or a separate "Immunity" system.

## Status
Accepted and Implemented.
