# ADR 028: Grid System

## Status
Accepted

## Context
Tactical combat requires tracking creature positions and calculating distances for movement and range checks.

## Decision
We will implement a 3D grid system using integer coordinates (X, Y, Z).
- **Position**: A struct representing a point in 3D space.
- **Distance Metric**: Chebyshev distance (Max(|dx|, |dy|, |dz|) * 5). This aligns with standard 5e rules where diagonal movement costs the same as orthogonal.
- **IGridManager**: Manages the mapping of Creatures to Positions.

## Consequences
- **Positive**: Enables tactical features (range, movement validation, AOE).
- **Negative**: Adds complexity to the engine state. Requires synchronization between GridManager and other components (e.g., if a creature moves via Action, GridManager must be updated).
