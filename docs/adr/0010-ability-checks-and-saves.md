# ADR 010: Ability Checks and Saving Throws

## Status
Accepted

## Context
The system needs a way to resolve ability checks and saving throws, which are core D20 mechanics.

## Decision
We implemented an `ICheckManager` interface responsible for rolling checks and saves.
- **Interface**: `ICheckManager` exposes `RollAbilityCheck(Ability)` and `RollSavingThrow(Ability)`.
- **Implementation**: `StandardCheckManager` uses an injected `IDiceRoller` to perform the rolls and add modifiers from `IAbilityScores`.
- **Result**: Returns `Result<int>` containing the total roll.

## Consequences
- **Positive**: Centralizes rolling logic, making it easy to mock for tests. Decouples rolling from the creature's main class.
- **Negative**: Adds another component to the creature composition.
