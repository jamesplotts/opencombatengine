# ADR 0002: Tactical AI Implementation (Tier 2)

## Status
Accepted

## Context
We needed to implement the second tier of AI behavior, "Tactical AI", to provide more challenging and realistic decision making than the basic "Zombie" AI (Tier 1). This tier should utilize creature statistics (`Intelligence`, `Wisdom`) to inform decisions.

## Decision
We implemented `TacticalAiController` which uses the following logic:

1. **Target Selection (Intelligence)**: 
    - Intelligent creatures (Int >= 8) prioritize targets that are easier to kill ("Finisher" behavior), scoring them based on a combination of Proximity and Low HP %.
    - Unintelligent creatures fall back to attacking the nearest enemy.
2. **Self Preservation (Wisdom)**:
    - Wise creatures (Wis >= 12) check their own HP. If critically low (< 30%), they will attempt to "Flee" (move away from enemies) rather than engaging.

## Consequences
- **Positive**: AI behavior is now varied based on stats. Smarter enemies are more dangerous.
- **Negative**: Adds complexity to the decision tree. Fleeing logic is basic (simple vector away from nearest enemy).
- **Future Work**: Tier 3 (Role-Based AI) will layer on top of this or replace it for specific classes.
