# 57. Combat Loop Refinement

Date: 2025-12-11

## Status

Accepted

## Context

The initial implementation of the Combat Loop (`StandardCombatManager` and `StandardTurnManager`) had two main limitations:
1.  **Hardcoded Win Condition**: The "Last Team Standing" logic was embedded directly in the manager, making it impossible to support other match types (e.g., "Kill the Boss", "Survive X Rounds").
2.  **Ghost Turns**: Dead creatures were still receiving turns, requiring them to manually pass or do nothing, which cluttered the flow.

## Decision

We decided to:

1.  **Extract Win Logic**: Create an `IWinCondition` strategy interface with a `Check(ICombatManager)` method.
2.  **Default Implementation**: Move the existing logic into `LastTeamStandingWinCondition` and make it the default for `StandardCombatManager`.
3.  **Refactor Turn Loop**: Update `StandardTurnManager.NextTurn()` to iterate through the initiative order until it finds a living creature or completes a full loop (preventing infinite loops if everyone is dead).

## Consequences

### Positive
- **Flexibility**: Developers can now inject custom win conditions for specific encounters.
- **Robustness**: The combat flow is smoother as dead participants are automatically skipped.
- **Testability**: Win conditions can be tested in isolation.

### Negative
- **API Change**: `StandardCombatManager` constructor now takes an optional `IWinCondition`.

## Compliance
- Verified with `CombatLoopTests` covering initiative, turn skipping, and custom win conditions.
