# ADR 039: Pathfinding Algorithm

## Status
Accepted

## Context
The `IGridManager` currently uses a simple straight-line interpolation (`GetLinePoints`) for `GetPath`. This does not account for obstacles (walls, other creatures) or movement costs (difficult terrain). We need a robust pathfinding algorithm to support tactical combat on a grid.

## Decision
We will implement the **A* (A-Star)** pathfinding algorithm in `StandardGridManager`.

### Key Decisions
1.  **Heuristic**: We will use **Chebyshev Distance** (`Max(|dx|, |dy|, |dz|)`) as the heuristic, consistent with D&D 5e's movement rules (diagonals cost the same as cardinals).
2.  **Cost Function**:
    -   Normal movement: 5 ft per square.
    -   Difficult Terrain: 10 ft per square.
    -   Obstacles: Impassable (infinite cost).
3.  **Occupied Squares**: Squares occupied by creatures will be treated as obstacles for the purpose of `GetPath`. (Future refinement: allow moving through allies).
4.  **3D Support**: The algorithm will support 3D coordinates (X, Y, Z).

## Consequences
-   **Performance**: A* is efficient but more costly than simple interpolation. For typical D&D map sizes (e.g., 50x50), it should be negligible.
-   **Accuracy**: Paths will correctly navigate around walls and minimize cost through difficult terrain.
-   **Complexity**: `StandardGridManager` logic increases significantly.
