# ADR 0053: Content Import Feature Integration

## Status
Accepted

## Context
We have implemented `SpellcastingFeature` and `ProficiencyFeature`, but they were not yet integrated into the content import process (JSON importers). We needed to ensure that when importing classes and races, these features are correctly created and assigned.

## Decision
We updated `JsonClassImporter` to parse the `proficiency` list in the class definition and create `ProficiencyFeature`s for each entry. These are assigned at level 1.
We verified that `JsonRaceImporter` automatically benefits from the updates to `FeatureFactory` and `FeatureParsingService`, correctly creating `SpellcastingFeature` and `ProficiencyFeature` from race entries.
We added integration tests to verify this behavior.

## Consequences
- Classes imported from JSON will now have proficiency features.
- Races imported from JSON will now have spellcasting and proficiency features if their entries describe them.
- `FeatureFactory` is now a critical component in the import pipeline.
