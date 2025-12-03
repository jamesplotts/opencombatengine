# ADR 026: Magic Item Import System

## Status
Accepted

## Context
Users need to import magic items from external sources (JSON) to populate their campaigns.

## Decision
We will implement a `JsonMagicItemImporter` that parses JSON data into `MagicItem` objects.
- **DTO**: `MagicItemDto` will mirror the JSON structure (5eTools format).
- **Mapping**: The importer will map DTO properties to `MagicItem` properties (`Name`, `Type`, `Attunement`, etc.).
- **Bonuses**: Simple bonuses (e.g. `bonusAc`, `bonusWeapon`) will be parsed into `IFeature` or `ICondition` where possible. Complex natural language parsing is out of scope.

## Consequences
- **Positive**: Enables rapid content population.
- **Negative**: Complex items with unique mechanics described in text won't be fully functional without manual coding/scripting.
