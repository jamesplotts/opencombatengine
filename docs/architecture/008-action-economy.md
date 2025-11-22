# 008 - Action Economy

## Context
In D&D 5e, a creature's turn is limited by the **Action Economy**: typically 1 Action, 1 Bonus Action, and 1 Reaction per round. Without tracking this, a creature could perform unlimited attacks or spells in a single turn.

## Decision
We decided to encapsulate this logic in an `IActionEconomy` interface composed into `ICreature`.

### Key Components
- **`IActionEconomy`**: Tracks the availability of Action, Bonus Action, and Reaction (booleans).
- **`StandardActionEconomy`**: Concrete implementation that defaults all resources to `true`.
- **Integration**:
    - `ICreature` exposes an `ActionEconomy` property.
    - `StandardCreature.StartTurn()` calls `ActionEconomy.ResetTurn()` to restore all resources.

## Consequences
- **Pros**:
    - **Enforcement**: Actions can now check `HasAction` before executing and call `UseAction()` to consume it.
    - **Automation**: Resources reset automatically at the start of the turn.
    - **Simplicity**: Boolean flags are sufficient for the core rules (though features like "Action Surge" may need to modify the implementation or add to it).
- **Cons**:
    - **Future Complexity**: Handling things like "Haste" (extra limited action) or "Action Surge" (extra full action) might require extending this interface or making the implementation more complex than simple booleans.

## Status
Accepted and Implemented.
