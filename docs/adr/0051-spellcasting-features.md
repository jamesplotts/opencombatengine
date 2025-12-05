# ADR 0051: Spellcasting Features

## Status
Accepted

## Context
Features need to be able to grant known spells to creatures (e.g., "You know the *dancing lights* cantrip"). We need a way to represent these features and look up the spells they grant.

## Decision
We implemented `SpellcastingFeature` which holds a list of `ISpell`s and adds them to the creature's `ISpellCaster` when applied.
We updated `ISpellCaster` to include `UnlearnSpell` to support feature removal.
We updated `FeatureFactory` to allow injecting an `ISpellRepository` and to parse spell grants from feature descriptions using regex.

## Consequences
- `FeatureFactory` now has a dependency on `ISpellRepository` (via static setter).
- `ISpellCaster` interface has changed (breaking change).
- Creatures can now dynamically learn spells from features.
