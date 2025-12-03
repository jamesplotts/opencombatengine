# 53. Basic Grid System

Date: 2025-11-26

## Status

Accepted

## Context

To support tactical combat, we need a way to track creature positions and calculate distances accurately. We need a grid system that can handle placement, movement, and distance checks.

## Decision

We will implement a 2D grid system using integer coordinates (X, Y).

### 1. GridPoint
- A simple struct `GridPoint(int X, int Y)`.
- Implements value equality.
- Provides distance calculation methods.

### 2. Distance Metric
- We will use **Chebyshev Distance** (Chessboard distance) as the default metric, consistent with standard 5e rules where diagonal movement costs the same as orthogonal movement (5ft).
- `Distance = Max(|x2-x1|, |y2-y1|) * 5`.

### 3. IGridManager
- Manages the mapping between `ICreature` and `GridPoint`.
- Enforces uniqueness: Only one creature per grid point (for now).
- Provides methods: `Place`, `Move`, `Remove`, `GetLocation`, `GetCreatureAt`.

## Consequences

- **Tactical Depth**: Enables range validation for attacks and spells.
- **Movement Validation**: Allows verifying if a move is legal (distance, occupancy).
- **2D Limitation**: Z-axis is ignored for now. Flying creatures will be treated as if on the ground or handled via separate "Elevation" property later.

## Alternatives Considered

- **Euclidean Distance**: More realistic but harder to count on a square grid. 5e uses simplified rules.
- **Hex Grid**: Better for equidistant movement, but square grids are more common in VTTs and easier to implement initially.
