# 14. Concrete Conditions

Date: 2025-11-22

## Status

Accepted

## Context

The system needs to support status effects (Conditions) that alter creature behavior or stats, such as Blinded, Charmed, or Poisoned.

## Decision

We implemented standard conditions using the `ICondition` interface.

- **Standard Conditions**: Implemented classes for `Blinded`, `Charmed`, `Deafened`, `Frightened`, `Grappled`, `Incapacitated`, `Invisible`, `Paralyzed`, `Petrified`, `Poisoned`, `Prone`, `Restrained`, `Stunned`, `Unconscious`.
- **Condition Manager**: `StandardConditionManager` stores active conditions.
- **Effects**: Conditions currently serve primarily as tags/flags. Future logic (e.g. Attack Rolls) will check `Conditions.HasCondition(ConditionType.Blinded)` to apply modifiers (Disadvantage).

## Consequences

- A comprehensive list of 5e conditions is now available.
- Conditions can be added/removed via `IConditionManager`.
- Logic for *applying* the effects of these conditions needs to be distributed to relevant systems (e.g. `AttackAction` checks for `Prone`).
