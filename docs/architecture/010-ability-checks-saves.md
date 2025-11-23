# 10. Ability Checks & Saving Throws

Date: 2025-11-22

## Status

Accepted

## Context

The system needs a way to resolve uncertain outcomes (Ability Checks) and resist negative effects (Saving Throws), core mechanics of the d20 system.

## Decision

We implemented `RollCheck` and `RollSave` methods on the `ICheckManager` interface (and `StandardCheckManager` implementation).

- **Ability Checks**: `RollCheck(Ability ability, int dc)` rolls a d20 + Ability Modifier + Proficiency (if applicable, though skill proficiency is not yet fully implemented).
- **Saving Throws**: `RollSave(Ability ability, int dc)` rolls a d20 + Ability Modifier + Proficiency (if proficient in that save).
- **Result**: Returns a `Result<bool>` indicating success or failure against the Difficulty Class (DC).

## Consequences

- Creatures can now perform basic d20 tests.
- Foundation laid for Skill Checks (which will wrap `RollCheck`).
- `ICheckManager` encapsulates the dice rolling logic for these tests, keeping `ICreature` cleaner.
