# ADR 033: Area of Effect

## Status
Accepted

## Context
Many spells and abilities affect multiple targets within a specific area (e.g., Fireball, Cone of Cold). We need a way to define these areas and query the grid for affected creatures.

## Decision
We will introduce an `IShape` interface to define geometric shapes on the grid.
`IGridManager` will provide a method `GetCreaturesInShape(Position origin, IShape shape, Position? direction)` to retrieve all creatures within the defined area.

### Shapes
- **Sphere**: Defined by a radius. Includes all points within distance `radius` from the origin.
- **Cube**: Defined by a size (length of side). Origin is typically a corner or center (D&D 5e rules vary, usually origin is a point on a face). For simplicity, we might center it or start from origin. Let's assume origin is the center for now, or follow 5e rules where origin is a point on a face. Actually, 5e Cube origin is a point on a face.
- **Cone**: Defined by length and angle (usually 53 degrees for 5e, or just width = length at end). Origin is the apex.
- **Line**: Defined by length and width. Origin is one end.

### Implementation Details
- Shapes will implement `bool Contains(Position origin, Position point, Position direction)`.
- `StandardGridManager` will iterate over all creatures and check if they are contained in the shape.

## Consequences
- **Positive**: Enables AOE spells. Decouples shape logic from the grid.
- **Negative**: Geometry calculations can be tricky on a discrete grid. We will use simplified approximations (e.g., Chebyshev distance for Sphere, simple bounds for Cube).
