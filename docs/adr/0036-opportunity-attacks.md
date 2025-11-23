# 36. Opportunity Attacks

Date: 2025-11-23

## Status

Accepted

## Context

Combat in the Open Combat Engine needs to support "Opportunity Attacks" (OA), a core mechanic in 5e where a creature can make a reaction attack when a hostile creature moves out of their reach.

This requires:
1.  Knowing a creature's "Reach" (usually 5ft, but 10ft for some weapons).
2.  Knowing the path a creature takes during movement (not just start/end).
3.  Interrupting movement to resolve the attack.
4.  Consuming the attacker's Reaction.

## Decision

We will implement Opportunity Attacks by enhancing the `MoveAction` and `GridManager`.

### 1. Reach Calculation
`IGridManager` will expose a `GetReach(ICreature creature)` method.
- Base reach is 5ft.
- If the creature has a weapon with the `Reach` property equipped, reach is 10ft.

### 2. Path Simulation
`IGridManager` will expose a `GetPath(Position start, Position end)` method to return the sequence of positions traversed.
`MoveAction` will use this path instead of just "teleporting" the creature.

### 3. Trigger Logic
During `MoveAction.Execute`:
- The system will iterate through the path step-by-step.
- For each step (Current -> Next), it will query for all hostile creatures.
- If the moving creature is within reach of a hostile at `Current` but NOT at `Next`:
    - And the hostile has a Reaction available:
        - An Opportunity Attack is triggered.

### 4. Attack Resolution
- The hostile's Reaction is consumed.
- An `AttackAction` (or equivalent logic) is executed immediately.
- If the attack hits, damage is applied.
- If the target survives, movement continues (unless a specific feature like Sentinel stops it, which is out of scope for this cycle).

## Consequences

### Positive
- Accurately models 5e mechanics.
- Adds tactical depth to positioning and movement.
- `GetPath` enables future features like trap triggering or hazard navigation.

### Negative
- Increases complexity of `MoveAction`.
- Performance cost to check for OAs at every step of movement (O(N steps * M creatures)). Given typical combat sizes, this is acceptable.

### Alternatives Considered
- **Event-based**: Trigger "OnMove" events. This is cleaner but might be harder to interrupt/control flow synchronously without a complex event system.
- **Post-move check**: Only check start/end. This fails if a creature circles around an enemy or moves through and out of reach. Step-by-step is required.
