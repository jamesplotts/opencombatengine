# 003 - Action System (Command Pattern)

## Context
Creatures need to perform actions (Attacks, Spells, etc.) and resolve them against targets. We need a system that decouples the *definition* of an action from its *execution* and allows for complex resolution logic (dice rolls, stats comparison).

## Decision
We decided to use a **Command Pattern** variation for Actions.

### Key Interfaces
- **`IAction`**: Represents a potential action (e.g., "Longsword Attack"). It has an `Execute(source, target)` method.
- **`ICombatStats`**: Encapsulates combat stats like AC, Initiative, and Speed.
- **`ActionResult`**: A record returning the outcome (Success/Fail, Message, Damage).

### Implementation
- **`AttackAction`**: A concrete implementation of `IAction` for weapon attacks.
    - Rolls d20 + AttackBonus vs Target AC.
    - Rolls Damage on hit.
    - Applies damage to Target via `IHitPoints.TakeDamage`.
- **`ActionType`**: Enum distinguishing Action, Bonus Action, Reaction, etc.

### 5e Rules Adherence
- **Movement**: Modeled as `Speed` in `ICombatStats`. Movement is a resource, not an Action type (though Dash is an Action).
- **Attacks**: Follow standard d20 + Mod vs AC rules.

## Consequences
- **Pros**:
    - **Extensible**: New actions (Spells, Feats) can be added by implementing `IAction`.
    - **Testable**: Actions can be tested in isolation with mocked dice/creatures.
- **Cons**:
    - **Complexity**: Requires careful management of dependencies (DiceRoller) within actions.

## Status
Accepted and Implemented.
