# ADR 014: Concrete Conditions

## Status
Accepted

## Context
The system needs to support standard conditions like Blinded, Prone, etc.

## Decision
We implemented a `StandardConditionManager` and specific condition classes.
- **Manager**: Handles adding/removing/ticking conditions.
- **Conditions**: Classes like `BlindedCondition` implement `ICondition` and define their effects (though effects are often applied by checking `HasCondition` elsewhere).

## Consequences
- **Positive**: Extensible condition system.
- **Negative**: Distributed logic (effects are often checked in `ResolveAttack` or `RollCheck` rather than encapsulated in the condition itself).
