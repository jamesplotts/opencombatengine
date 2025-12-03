# 43. Condition Effects

Date: 2025-11-25

## Status

Accepted

## Context

The OpenCombatEngine needs a way to automatically apply mechanical effects (like Advantage/Disadvantage on attacks) when standard conditions (Blinded, Prone, Restrained, etc.) are applied to a creature. Previously, conditions were just markers without mechanical impact. We recently implemented an Active Effects system (Cycle 28) which is capable of handling such modifiers.

## Decision

We will link the Conditions System with the Active Effects System.

1.  **Condition Factory**: We will create a `ConditionFactory` to centralize the creation of standard conditions. This factory will attach specific `IActiveEffect` instances to the `Condition` object upon creation.
2.  **Condition Effects**: We will implement specific `IActiveEffect` classes for common condition mechanics:
    *   `AdvantageOnIncomingAttacksEffect`: Grants advantage to attacks targeting the affected creature.
    *   `DisadvantageOnOutgoingAttacksEffect`: Imposes disadvantage on attacks made by the affected creature.
    *   `StatBonusEffect`: (Already exists) Can be used for simple stat changes if needed.
3.  **Manager Integration**: The `StandardConditionManager` will be updated to:
    *   Register the condition's effects with the creature's `IEffectManager` when a condition is added.
    *   Unregister the effects when the condition is removed.
4.  **Action Integration**: `AttackAction` will be updated to query the `IEffectManager` (via `StatType`s like `AttackAdvantage`, `IncomingAttackAdvantage`) to determine if advantage or disadvantage applies.

## Consequences

*   **Pros**:
    *   Automates the application of condition mechanics, reducing manual bookkeeping.
    *   Leverages the existing Active Effects system, avoiding code duplication.
    *   Extensible: New conditions can be added by defining new effects and updating the factory.
*   **Cons**:
    *   Increases coupling between Conditions and Effects systems.
    *   Requires careful management of effect lifecycles to ensure they are removed when conditions expire.

## Implementation Details

*   `ICondition` now has an `IEnumerable<IActiveEffect> Effects` property.
*   `ConditionFactory.Create(ConditionType type)` returns a `Condition` with the appropriate effects populated.
*   `StandardConditionManager` handles the synchronization between `ActiveConditions` and `IEffectManager`.
