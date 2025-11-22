# 001 - Creature Interfaces and Components

## Context
The system required a way to represent various entities (PCs, NPCs, Monsters) in the combat engine. The original proposal was an `IEntity` interface. We needed a flexible, extensible architecture that avoids deep inheritance hierarchies (e.g., `Dragon : Monster : Entity`).

## Decision
We decided to use a **Composition-based** architecture with `ICreature` as the primary container interface.

### Key Interfaces
- **`ICreature`**: The root interface for any being. It composes other capabilities.
- **`IAbilityScores`**: Encapsulates the six standard ability scores (Str, Dex, Con, Int, Wis, Cha) and modifier logic.
- **`IHitPoints`**: Encapsulates health state (Current, Max, Temp).

### Concrete Implementations
We implemented "Standard" versions of these interfaces to support SRD 5.1 rules:
- **`StandardAbilityScores`**: Immutable record that calculates modifiers using `floor((Score - 10) / 2)`.
- **`StandardHitPoints`**: Manages health state, enforcing non-negative values and death state.
- **`StandardCreature`**: A concrete implementation of `ICreature` that holds these components.

## Consequences
- **Pros**:
    - Flexible: Can easily add new components (e.g., `ISpeed`, `ISenses`) without breaking existing code.
    - Testable: Components can be tested in isolation.
    - Extensible: Different implementations of `IHitPoints` (e.g., one with damage reduction) can be swapped in.
- **Cons**:
    - slightly more verbose instantiation than a simple class hierarchy.

## Status
Accepted and Implemented.
