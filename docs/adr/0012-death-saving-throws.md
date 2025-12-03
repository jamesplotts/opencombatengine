# ADR 012: Death Saving Throws

## Status
Accepted

## Context
When a creature reaches 0 HP, it must make death saving throws to determine if it stabilizes or dies.

## Decision
We implemented death saving throw logic within the `StartTurn` lifecycle method.
- **State**: `IHitPoints` tracks `DeathSaveSuccesses` and `DeathSaveFailures`.
- **Automation**: `StandardCreature.StartTurn` checks if HP is 0 and not stable, then rolls a death save using `ICheckManager`.
- **Rules**: Handles critical successes (regain 1 HP) and critical failures (2 failures).

## Consequences
- **Positive**: Automates a tedious part of combat tracking. Ensures rules compliance.
- **Negative**: "Hidden" logic in `StartTurn` might surprise developers if not documented.
