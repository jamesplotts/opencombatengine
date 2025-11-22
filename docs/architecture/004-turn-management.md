# 004 - Turn Management (Cyclic Initiative)

## Context
Combat requires a structured flow where creatures take turns acting. We need a system to determine turn order and track rounds.

## Decision
We decided to implement a **Cyclic Initiative** system.

### Key Interfaces
- **`ITurnManager`**: Manages the state of combat (Current Round, Current Creature, Turn Order).
- **`InitiativeRoll`**: Represents a creature's initiative result (Total + Dex Score).

### Implementation
- **`StandardTurnManager`**:
    - **StartCombat**: Rolls initiative (d20 + Bonus) for all creatures.
    - **Sorting**: Sorts by Total (Descending).
    - **Tie-Breaker**: Uses Dexterity Score (Descending) to break ties, adhering to 5e rules.
    - **Cycling**: `NextTurn()` advances the index. When the end of the list is reached, it increments `CurrentRound` and loops back to the start.

## Consequences
- **Pros**:
    - **Simple**: Easy to understand and predict.
    - **Standard Compliant**: Follows standard 5e initiative rules.
- **Cons**:
    - **Static**: Doesn't currently support dynamic re-ordering (e.g., holding actions), though this can be added later.

## Status
Accepted and Implemented.
