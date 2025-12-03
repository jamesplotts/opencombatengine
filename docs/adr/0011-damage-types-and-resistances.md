# ADR 011: Damage Types and Resistances

## Status
Accepted

## Context
Combat involves various types of damage (Fire, Slashing, etc.) and creatures may be resistant, vulnerable, or immune to them.

## Decision
We introduced a `DamageType` enum and updated `ICombatStats` and `IHitPoints`.
- **Enum**: `DamageType` covers standard 5e types.
- **Stats**: `ICombatStats` maintains `Resistances`, `Vulnerabilities`, and `Immunities` sets.
- **Logic**: `TakeDamage` logic checks these sets to modify damage amount (halve, double, or zero) before applying to HP.

## Consequences
- **Positive**: Supports complex damage interactions standard in 5e.
- **Negative**: Damage calculation logic becomes slightly more complex.
