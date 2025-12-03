# ADR 0044: Magic Item Active Abilities (Spells)

## Status
Accepted

## Context
Magic items in D&D 5e often allow the user to cast spells (e.g., "Staff of Fire" casts "Fireball" or "Burning Hands").
Currently, our `MagicItem` implementation supports `IMagicItemAbility`, but we lack a mechanism to link these abilities to actual `ISpell` definitions during import.
The `JsonMagicItemImporter` parses item data, but spells are imported separately by `JsonSpellImporter`. We need a way to resolve spell references (by name) when creating magic items.

## Decision
We will introduce an `ISpellRepository` interface to decouple spell lookup from the importer.
We will implement a simple `InMemorySpellRepository` for now.
We will create a specific `CastSpellFromItemAbility` implementation of `IMagicItemAbility` that holds a reference to the repository and the spell name, resolving the spell lazily at runtime (or eagerly if possible, but lazy avoids load order issues).
We will update `JsonMagicItemImporter` to accept `ISpellRepository` as a dependency.

## Consequences
- **Breaking Change**: `JsonMagicItemImporter` constructor will change, requiring updates to tests and DI configuration.
- **Dependency**: Magic item import now depends on the existence of a spell repository.
- **Flexibility**: This approach allows for different spell storage backends (database, API, etc.) in the future.

## Technical Details
### ISpellRepository
```csharp
public interface ISpellRepository
{
    Result<ISpell> GetSpell(string name);
    void AddSpell(ISpell spell);
}
```

### CastSpellFromItemAbility
Will use `ISpellRepository` to find the spell when `Execute` is called, then delegate to `CastSpellAction`.
