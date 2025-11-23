# ADR 034: Movement Cost

## Status
Accepted

## Context
Movement in combat is not always uniform. "Difficult Terrain" (rubble, swamp, steep stairs) slows down creatures, costing 2 feet of movement for every 1 foot traveled (or 10ft per 5ft square). We need to support this in the Grid System.

## Decision
We will extend `IGridManager` to support "Movement Cost".
- `void AddDifficultTerrain(Position position)`: Marks a cell as difficult terrain.
- `int GetMovementCost(Position from, Position to)`: Calculates the cost to move between two adjacent cells.
    - Default cost: 5.
    - If `to` is difficult terrain: 10.
- `int GetPathCost(Position start, Position end)`: Calculates the total cost of a path.
    - For now, we will assume a straight line path (Bresenham) and sum the costs of entering each cell.

`MoveAction` will be updated to validate movement based on `GetPathCost(start, end) <= AvailableMovement`.

## Consequences
- **Positive**: Supports tactical terrain usage.
- **Negative**: Pathfinding becomes more complex if we want to find the *optimal* path around difficult terrain. For now, we assume straight-line movement for simplicity, or the user specifies the path step-by-step (which our current `MoveAction` doesn't fully support yet - it takes a destination).
    - *Refinement*: If `MoveAction` takes a destination, and we calculate cost based on a straight line, players might get stuck on difficult terrain they could walk around.
    - *Mitigation*: For this cycle, we stick to straight line cost. Future cycles (Pathfinding) can implement A* to find the cheapest path.

## Technical Details
- `StandardGridManager` will use a `HashSet<Position> _difficultTerrain`.
- `GetPathCost` will iterate through the points returned by the line algorithm (excluding start, including end) and sum costs.
