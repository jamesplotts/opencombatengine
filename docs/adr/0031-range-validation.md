# ADR 031: Range Validation

## Status
Accepted

## Context
Attacks currently do not validate range. With the Grid System in place, we can now enforce range restrictions.

## Decision
We will add a `Range` property (in feet) to `AttackAction` and `MonsterAttackAction`.
When executing an attack:
1. If an `IGridManager` is available in the `IActionContext`:
    - Retrieve the positions of the source and target creatures.
    - Calculate the distance between them using the Grid's distance metric (Chebyshev).
    - If the distance exceeds the `Range`, the action fails.
2. If no Grid is available, range validation is skipped (or assumed successful).

## Consequences
- **Positive**: Enforces spatial rules. Prevents melee attacks from across the map.
- **Negative**: Requires updating action constructors. Requires Grid for validation (fallback is lenient).
