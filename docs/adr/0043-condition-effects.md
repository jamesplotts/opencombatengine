# 43. Condition Effects

Date: 2025-11-24

## Status

Accepted

## Context

The **Conditions System** (Cycle 5) allows creatures to have conditions like "Blinded" or "Prone", but these are currently just semantic flags. The **Active Effects System** (Cycle 28) provides a mechanism to modify rolls and stats (e.g., Advantage/Disadvantage). We need to link these two systems so that conditions automatically apply their rules-as-written mechanical effects.

## Decision

We will integrate `ICondition` with `IActiveEffect` by:

1.  Adding an `IEnumerable<IActiveEffect> Effects { get; }` property to the `ICondition` interface.
2.  Updating `StandardConditionManager` to automatically register a condition's effects with the creature's `IEffectManager` when the condition is added, and remove them when the condition is removed.
3.  Creating a `ConditionFactory` to generate standard conditions (Blinded, Prone, etc.) pre-populated with their specific `IActiveEffect` implementations.
4.  Implementing specific `IActiveEffect` classes for common condition mechanics (e.g., `AdvantageOnIncomingAttacksEffect`, `DisadvantageOnOutgoingAttacksEffect`).

## Consequences

### Positive
-   **Automation**: Conditions will automatically enforce their rules (e.g., a Blinded creature will automatically roll attacks with disadvantage).
-   **Consistency**: Leveraging the existing Active Effects system avoids duplicating logic for roll modification.
-   **Extensibility**: New conditions can be easily added by defining their effects.

### Negative
-   **Complexity**: `StandardConditionManager` now depends on `IEffectManager`.
-   **Circular Dependencies**: Must be careful not to create circular dependencies between Condition and Effect systems (though they should be separate enough).

## Alternatives Considered

-   **Hardcoding in Actions**: We could check `creature.Conditions.HasCondition("Blinded")` inside `AttackAction`, but this would scatter condition logic across many actions and be hard to maintain.
-   **Event-Based**: We could use events, but Active Effects are already designed for this exact "modify roll/stat" purpose.
