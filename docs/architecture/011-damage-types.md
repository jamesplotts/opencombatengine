# 11. Damage Types & Resistances

Date: 2025-11-22

## Status

Accepted

## Context

Combat requires differentiating between types of damage (Fire, Slashing, etc.) to support Resistances, Vulnerabilities, and Immunities.

## Decision

We introduced a `DamageType` enum and updated the `IHitPoints.TakeDamage` method.

- **DamageType Enum**: Includes standard types (Bludgeoning, Piercing, Slashing, Fire, Cold, etc.).
- **TakeDamage Update**: Now accepts `DamageType` as a parameter.
- **Resistance Logic**: `StandardHitPoints` (or `StandardCreature` via `ICombatStats`) checks for resistance/vulnerability before applying damage.
    - Resistance: Halves damage.
    - Vulnerability: Doubles damage.
    - Immunity: Negates damage.

## Consequences

- Damage calculations are now type-aware.
- `DamageTakenEventArgs` includes the `DamageType` for event subscribers (UI, logs).
- Future features like "Resistance to Non-Magical Attacks" can be built on this enum.
