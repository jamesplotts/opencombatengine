# 55. Reaction System

Date: 2025-12-06

## Status

Accepted

## Context

The engine previously handled reactions (specifically Opportunity Attacks) via a static helper class `OpportunityAttack` that was tightly coupled to the movement logic and difficult to extend. There was no unified "Reaction" concept, meaning adding new reactions (like *Shield* spell or *Sentinel* feat) would require hacking multiple places.

With the introduction of the Core Event System (ADR 0054), we now have the infrastructure to implement an event-driven reaction system.

## Decision

We have implemented a `ReactionSystem` based on the observable "Push" model.

### 1. IReaction
We defined an interface `IReaction` that encapsulates logic for a single reaction type:
- `bool CanReact(object eventArgs, IReactionContext context)`: Checks triggers/conditions.
- `Result<ActionResult> React(object eventArgs, IReactionContext context)`: Executes the reaction.

### 2. IReactionManager
We introduced `IReactionManager` as a component on `ICreature`.
- **Responsibilities**: 
  - Maintains a list of available reactions.
  - Listens to system events (e.g., `GridManager.CreatureMoved`).
  - When an event occurs, it iterates through available reactions and executes the first valid one (simplistic strategy for now, can be expanded to prompt user).

### 3. GridManager Integration
The `StandardGridManager` now explicitly checks for reactions after a creature moves.
- It iterates all *other* creatures on the grid.
- It calls `CheckReactions` on their `ReactionManager`.
- This ensures that when Creature A moves, Creature B is immediately given a chance to react.

### 4. Opportunity Attack Implementation
We implemented `OpportunityAttackReaction` as a concrete `IReaction`.
- **Trigger**: `MovedEventArgs` where the moving creature leaves the reactor's reach.
- **Action**: Consumes the reactor's "Reaction" resource (Action Economy) and logs an attack success. (Full attack resolution deferred to future combat polish or integration with `AttackAction`).

## Consequences

### Positive
- **Extensibility**: New reactions can be added by simply implementing `IReaction` and adding it to the manager.
- **Decoupling**: Movement logic doesn't need to know *what* reactions exist, only that it should notify the system.
- **Standardization**: All reactions follow the same flow.

### Negative
- **Push Overhead**: The GridManager iterating all creatures on every move could be slow with hundreds of entities (optimization needed later, e.g., spatial partitioning query).

## Compliance
- Follows the Event System pattern established in ADR 0054.
- Uses `IActionEconomy` to track resource usage.
