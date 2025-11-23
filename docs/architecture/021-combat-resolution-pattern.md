# 21. Combat Resolution Pattern

Date: 2025-11-22

## Status

Accepted

## Context

The previous combat implementation had `AttackAction` directly calculating hits and applying damage to the target's `HitPoints`. This made it difficult to implement defensive features like Resistances, Immunities, Parry, or Shield spell, as the attacker had to know too much about the defender, or the logic had to be scattered.

## Decision

We adopted the "Attack Object" pattern (Command/Context pattern).

1.  **AttackResult**: An object representing the *attempted* attack. It contains:
    - Source and Target.
    - Attack Roll (Total).
    - IsCritical flag.
    - List of `DamageRoll` (Amount + Type).
2.  **ResolveAttack**: A method on `ICreature` that accepts an `AttackResult`.
    - The *defender* determines if the attack hits (checking AC vs Attack Roll).
    - The *defender* applies mitigation (Resistances, etc.).
    - The *defender* applies the final damage to their HP.
    - Returns an `AttackOutcome` (Hit/Miss, Damage Dealt) to the caller.

## Consequences

- **Decoupling**: Attackers just "send" an attack; Defenders "resolve" it.
- **Extensibility**: We can easily add logic to `ResolveAttack` for Resistances, Uncanny Dodge, etc., without changing every Action.
- **Observability**: The `AttackResult` object can be inspected or logged easily.
- **Breaking Change**: `ICreature` interface changed, requiring updates to all implementations and mocks.
