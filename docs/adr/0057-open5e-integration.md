# 57. Open5e API Integration

Date: 2025-12-11

## Status

Accepted

## Context

The engine requires a rich source of content (Spells, Monsters) to be useful. Creating this content manually is time-consuming. The Open5e API provides a free, open-source compliant database of 5th Edition SRD content.

## Decision

We integrated the Open5e API directly into the engine using a dedicated `Open5eContentSource`.

Key components:
*   `Open5eClient`: A typed HTTP client for fetching data.
*   `Open5eDtos`: DTOs mirroring the API's JSON schemas.
*   `Open5eAdapter`: Adapts API DTOs to our internal `SpellDto`/`MonsterDto`.
*   `SpellMapper`/`MonsterMapper`: Refactored static mappers that convert DTOs to Domain Entities.

We extracted the mapping logic from the existing `JsonSpellImporter` and `JsonMonsterImporter` to allow reuse.

## Consequences

*   **Positive**: The engine can now dynamically load "fireball", "ancient-red-dragon", and other SRD content without manual data entry.
*   **Positive**: Improved modularity by separating DTO-to-Domain mapping logic from file import logic.
*   **Negative**: Dependency on an external API for some content (though offline fallbacks or caching can be added later).
