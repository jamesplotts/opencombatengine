# 52. Environmental Effects (Abstract)

Date: 2025-11-26

## Status

Accepted

## Context

Combat often involves environmental factors like cover, difficult terrain, and poor visibility. We need to model these effects abstractly before implementing a full grid system.

## Decision

We will introduce abstract properties to represent these states in the context of an action or creature state.

### 1. Cover
- Represented by `CoverType` enum: `None`, `Half` (+2 AC/Dex), `ThreeQuarters` (+5 AC/Dex), `Total` (Untargetable).
- Included in `IActionContext` as `TargetCover`.
- `StandardCombatManager` will apply AC bonuses based on this context.

### 2. Difficult Terrain
- Represented by a boolean flag `IsInDifficultTerrain` on `IMovement`.
- `StandardMovement` will double movement cost when this flag is true.

### 3. Obscurement
- Represented by `ObscurementType` enum: `None`, `Lightly`, `Heavily`.
- Included in `IActionContext` as `TargetObscurement`.
- `StandardCombatManager` or check managers can use this to apply disadvantage (e.g., heavily obscured = blinded condition effects).

## Consequences

- **Abstract Representation**: Allows testing rules without a map.
- **Context Dependency**: Cover is relative (Attacker -> Target), so it lives in the Action Context, not on the creature itself (though a creature might be "behind cover" generally, usually it's relative to the source).
- **Future Grid Integration**: When grid is added, these values will be calculated dynamically based on geometry. For now, they are manually set/mocked in context.

## Alternatives Considered

- **Conditions**: Implementing Cover as a Condition. This is messy because cover is directional/relative, not a persistent state on the creature for all attackers.
- **Wait for Grid**: Delaying until grid system. We want to verify combat math (AC bonuses) independently of geometry.
