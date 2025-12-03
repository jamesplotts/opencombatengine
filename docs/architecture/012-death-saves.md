# 12. Death Saving Throws

Date: 2025-11-22

## Status

Accepted

## Context

When a creature reaches 0 Hit Points, they shouldn't necessarily die immediately. The 5e ruleset uses Death Saving Throws to determine fate.

## Decision

We implemented Death Saving Throw logic within the `StartTurn` method of `StandardCreature`.

- **Trigger**: If `CurrentHP <= 0` and not `Dead` or `Stabilized` at start of turn.
- **Mechanic**: Roll d20.
    - 10+: Success.
    - <10: Failure.
    - 1: Two Failures.
    - 20: Regain 1 HP (become conscious).
- **Tracking**: `IHitPoints` tracks `DeathSaveSuccesses` and `DeathSaveFailures`.
- **Outcome**:
    - 3 Successes: Stabilized.
    - 3 Failures: Dead (invokes `Died` event).

## Consequences

- Turn lifecycle now includes automatic death save handling.
- `IHitPoints` state must persist death save counts.
- Healing resets death saves.
