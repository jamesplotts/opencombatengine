# ADR 035: Flanking

## Status
Accepted

## Context
Tactical positioning is key in combat. Flanking an enemy (surrounding them) should provide an advantage.
To determine flanking, we need to know which creatures are allies.

## Decision
We will introduce a `Team` property (string) to `ICreature`.
- Creatures with the same `Team` are allies.
- Creatures with different `Team`s are enemies (simplified).

We will add `bool IsFlanked(ICreature target, ICreature attacker)` to `IGridManager`.
- A target is flanked if:
    1.  The attacker is adjacent to the target.
    2.  There is an ally of the attacker (same Team) on the exact opposite side of the target.
    3.  "Opposite side" means a line drawn from attacker's center to target's center passes through opposite square/cube.
    - Simplified for Grid: If Attacker is at (x, y) and Target is at (tx, ty), the Flanking Ally must be at (tx + (tx - x), ty + (ty - y)).
    - Example: Attacker (0,0), Target (1,0). Flanker must be at (2,0).
    - Example: Attacker (0,0), Target (1,1). Flanker must be at (2,2).

`AttackAction` will check `IsFlanked` and grant Advantage if true.

## Consequences
- **Positive**: Encourages tactical movement.
- **Negative**: "Conga line" of flanking (A-B-A-B).
- **Future**: Optional rule? For now, core mechanic.
