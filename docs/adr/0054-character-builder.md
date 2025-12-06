# 54. Character Builder and Leveling Logic

Date: 2025-12-06

## Status

Accepted

## Context

We needed a standardized way to construct `ICreature` instances that adheres to the game rules (5th Edition OGL-like), specifically regarding:
1.  **Character Creation**: Combining Race, Class, and Ability Scores to produce a valid creature.
2.  **Leveling Up**: Handling Hit Point (HP) increases (Max Die at Level 1, Average/Roll thereafter) and applying features.
3.  **Component Dependencies**: Core components like `StandardCombatStats`, `StandardSpellCaster`, and `StandardCheckManager` relied on static values or partial interfaces (`IAbilityScores`), leading to "stale" data issues where changing a creature's stat didn't update derived values (e.g., Skill modifiers).

## Decision

### 1. Refactor Components to Depend on `ICreature`
We modified `StandardCombatStats`, `StandardSpellCaster`, and `StandardCheckManager` to take `ICreature` in their constructors.
*   **Why**: This ensures they always access the current state of the creature (e.g., `creature.AbilityScores`), allowing dynamic updates (like buffs or ASIs) to propagate immediately.
*   **Implication**: This introduced a circular dependency during instantiation (Creature needs CheckManager, CheckManager needs Creature). We solved this by passing `this` inside the `StandardCreature` constructor to its components.

### 2. Implement `CharacterBuilder`
We introduced a `CharacterBuilder` class that uses the Builder pattern.
*   **Function**: Steps through setting Name, Race, Class, and Ability Scores.
*   **Build Process**: Creates a blank `StandardCreature`, then applies the Class (triggering Level 1 logic), and fixes up HP.

### 3. Level 1 HP Logic
We updated `StandardLevelManager.LevelUp` and `CharacterBuilder` to enforce the rule: **Level 1 HP = Hit Die Max + CON Modifier**.
*   Subsequent levels use Average + CON (or rolled, if configured).

### 4. `StandardCreature` Updates
*   Added `ApplyAbilityScoreIncreases` to handle Race bonuses.
*   Updated constructor to initialize components with `this`.

## Consequences

*   **Positive**:
    *   **Robustness**: Character creation enforces rules and dependencies.
    *   **Dynamic**: Stat updates propagate correctly.
*   **Negative**:
    *   **Complexity**: Circular dependencies in constructor required careful handling.
    *   **Refactoring**: Significant test updates were required.
