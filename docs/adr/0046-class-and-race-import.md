# ADR 0046: Class and Race Import

## Status
Accepted

## Context
We have defined `IClassDefinition` and `IRaceDefinition` in the previous cycle. Now we need a way to populate these definitions from external data sources, specifically JSON files following the 5e.tools format (or a subset thereof).

## Decision
We will implement `JsonClassImporter` and `JsonRaceImporter` classes.
These will use `System.Text.Json` to deserialize JSON data into DTOs (`ClassDto`, `RaceDto`) and then map them to our domain models (`ClassDefinition`, `RaceDefinition`).

### Class Import
- We will parse basic class info: Name, Hit Die.
- We will attempt to parse features from `classTableGroups` or similar structures if available, but primarily focus on the core definition first.
- Complex feature parsing (e.g. "Sneak Attack" logic) will likely require a more robust feature factory or lookup system in the future. For now, we may create generic text-based features or placeholders.

### Race Import
- We will parse: Name, Speed, Size, Ability Score Increases.
- Racial features will be parsed from `entries`.

## Consequences
- We can now load classes and races from data files.
- The importers will need to be robust to the variability of the 5e.tools JSON format (e.g. `speed` can be an int or an object).
- This decouples content from code, allowing for easier expansion.

## Technical Details
- DTOs will be placed in `OpenCombatEngine.Implementation.Content.Dtos`.
- Importers will implement `IContentImporter<T>`.
