# 19. Content Import System

Date: 2025-11-22

## Status

Accepted

## Context

To support a rich library of game content (Spells, Monsters, Items) without hardcoding everything in C#, we need a way to import data from external files. The community has standardized around JSON formats, specifically the schema used by 5eTools.

## Decision

We implemented a `JsonSpellImporter` that targets the 5eTools JSON schema.

- **Interface**: `IContentImporter<T>` defines the contract `Import(string data)`.
- **Format**: JSON.
- **Schema**: 5eTools Spell Schema (subset).
    - Maps `name`, `level`, `school`, `time`, `range`, `components`, `duration`, `entries`.
    - Handles data normalization (e.g. school codes "E" -> "Evocation").
- **DTOs**: `SpellDto`, `TimeDto`, etc. mirror the JSON structure.
- **Library**: `System.Text.Json` for parsing.

## Consequences

- Users can import spells from widely available JSON files.
- The engine is decoupled from specific content data.
- Future importers (Monsters, Items) can follow this pattern.
- We accept a dependency on the external schema structure (if 5eTools changes, we may need to update DTOs).
