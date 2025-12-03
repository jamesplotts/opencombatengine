# ADR 0045: Character Classes & Races

## Status
Accepted

## Context
To support character creation and progression, we need a structured way to represent Character Classes (e.g., Fighter, Wizard) and Races (e.g., Human, Elf).
Currently, `ILevelManager` uses simple string identifiers for classes, which prevents us from attaching logic (like features, hit dice, etc.) to them.
We also lack a formal definition for Races.

## Decision
We will introduce `IClassDefinition` and `IRaceDefinition` interfaces.
- `IClassDefinition` will encapsulate class data: Name, Hit Die, and Features by Level.
- `IRaceDefinition` will encapsulate race data: Name, Speed, Size, Ability Score Increases, and Racial Features.

We will update `ILevelManager` to use `IClassDefinition` instead of strings for class tracking.
We will update `ICreature` to include an `IRaceDefinition` property.

## Consequences
- **Breaking Change**: `ILevelManager.Classes` will change signature. Existing code using string class names will need updates.
- **Progression**: Leveling up will now be able to automatically apply features based on the class definition.
- **Extensibility**: This structure allows for easy addition of new classes and races, including importing them from external sources.

## Technical Details
### IClassDefinition
```csharp
public interface IClassDefinition
{
    string Name { get; }
    int HitDie { get; }
    IReadOnlyDictionary<int, IEnumerable<IFeature>> FeaturesByLevel { get; }
}
```

### IRaceDefinition
```csharp
public interface IRaceDefinition
{
    string Name { get; }
    int Speed { get; }
    Size Size { get; }
    IReadOnlyDictionary<Ability, int> AbilityScoreIncreases { get; }
    IEnumerable<IFeature> RacialFeatures { get; }
}
```
