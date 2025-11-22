# 002 - Creature Serialization (Memento Pattern)

## Context
We needed a way to serialize creature state so developers can save/load creatures using their preferred method (JSON, XML, Database, etc.). The legacy `ISerializable` interface is deprecated and tied to `BinaryFormatter`, which is insecure.

## Decision
We decided to implement the **Memento Pattern** using Data Transfer Objects (DTOs).

### Key Concepts
- **`IStateful<TState>`**: An interface for objects that can export their state.
- **State Records**: Pure data records (e.g., `CreatureState`, `AbilityScoresState`) that contain only serializable primitives (int, string, Guid).
- **Constructors**: Components provide a constructor that accepts their corresponding State object to restore functionality.

### Implementation
- **`CreatureState`**: Contains `Id`, `Name`, `AbilityScores`, and `HitPoints`.
- **`StandardCreature`**: Implements `IStateful<CreatureState>`.
- **Serialization**: The library does *not* perform serialization itself. It exposes the State object, which the consumer can serialize using `System.Text.Json`, `Newtonsoft.Json`, or any other serializer.

## Consequences
- **Pros**:
    - **Serializer Agnostic**: Works with any serializer.
    - **Secure**: No `BinaryFormatter` risks.
    - **Encapsulated**: Internal logic isn't exposed; only the state data is.
- **Cons**:
    - Requires maintaining parallel "State" classes for every stateful component.

## Status
Accepted and Implemented.
