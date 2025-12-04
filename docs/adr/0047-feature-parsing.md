# ADR 0047: Feature Parsing

## Status
Accepted

## Context
Classes and Races have features (e.g., "Darkvision", "Sneak Attack"). These are represented in JSON data (5e.tools format) as recursive entries. We need a way to parse these into our `IFeature` system.

## Decision
We implemented a `FeatureParsingService` (static) that recursively parses JSON elements.
We created a `TextFeature` class (implementing `IFeature`) to hold the name and description of features that do not yet have specific mechanical implementations.

## Consequences
- We can now import racial features as informational items on the character sheet.
- Future work will involve mapping specific feature names to concrete `IFeature` implementations with logic (e.g., `SneakAttackFeature`).
- The `FeatureParsingService` is a central place for this mapping logic.

## Technical Details
- `FeatureParsingService` handles `entries` arrays and nested objects.
- `TextFeature` is a passive feature.
