# ADR 0052: Proficiency Features

## Status
Accepted

## Context
Features need to be able to grant proficiencies in skills and saving throws (e.g., "Proficiency in Stealth", "Proficiency in Constitution saving throws"). We need a way to manage these proficiencies in the creature model and apply them during checks.

## Decision
We updated `ICheckManager` to include methods for adding, removing, and checking proficiencies for skills and saving throws.
We updated `StandardCheckManager` to store these proficiencies and apply the creature's proficiency bonus to rolls when applicable.
We implemented `ProficiencyFeature` which calls these methods on the creature's check manager.
We updated `FeatureFactory` to parse proficiency grants from feature descriptions.

## Consequences
- `ICheckManager` interface has changed (breaking change).
- `StandardCheckManager` now maintains state for proficiencies.
- Creatures can now dynamically gain proficiencies from features.
