# ADR 032: Line of Sight

## Status
Accepted

## Context
Combat requires visibility rules. Creatures should not be able to target entities through solid obstacles (walls, pillars, etc.).

## Decision
We will extend the `IGridManager` to support **Obstacles** and **Line of Sight (LOS)** checks.

### Obstacles
- The Grid will maintain a set of "obstructed" positions.
- Obstacles block movement (implicitly, though movement validation might need explicit updates later) and LOS.

### Line of Sight Algorithm
- We will use a 3D line tracing algorithm (Bresenham's algorithm or a variant like Amanatides-Woo for voxel traversal) to determine if a clear path exists between two points.
- `HasLineOfSight(start, end)` will return `true` if no obstacles exist on the line segment connecting `start` and `end`.
- The start and end positions themselves are generally excluded from the obstruction check (you can see the target even if the target is standing *in* a space, unless the space itself is fully opaque, but for now we assume creatures occupy spaces that are otherwise valid). Actually, usually the target's space doesn't block seeing the target.

## Consequences
- **Positive**: Adds tactical depth. Prevents attacks through walls.
- **Negative**: Computational cost of line tracing (though negligible for turn-based combat). Complexity of 3D line algorithms.
