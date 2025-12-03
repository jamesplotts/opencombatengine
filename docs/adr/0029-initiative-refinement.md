# ADR 029: Initiative System Refinement

## Status
Accepted

## Context
The initiative system needs to handle ties robustly and be extensible for variant rules (e.g., Speed Factor Initiative).

## Decision
We will extract the initiative sorting logic into an `IInitiativeComparer` strategy.
- **Interface**: `IInitiativeComparer : IComparer<InitiativeRoll>`
- **Standard Implementation**: `StandardInitiativeComparer` sorts by:
    1. Total Roll (Descending)
    2. Dexterity Score (Descending)
    3. Random/Deterministic Tie-Breaker (e.g., Creature ID)

## Consequences
- **Positive**: Decouples sorting logic from the Turn Manager. Allows easy swapping of initiative rules.
- **Negative**: Adds a small amount of complexity (dependency injection).
