# ADR 0048: Feature Factory

## Status
Accepted

## Context
We need to parse features from JSON (e.g., "Darkvision", "Speed +10") into concrete `IFeature` implementations that have mechanical effects, rather than just text descriptions.

## Decision
We implemented a `FeatureFactory` that parses feature names and descriptions to create specific `IFeature` instances.
We introduced:
- `SenseFeature`: For senses like Darkvision.
- `AttributeBonusFeature`: For simple stat bonuses (implemented via `StatBonusEffect`).
- `FeatureFactory`: To map strings to these features.

## Consequences
- Importing content now results in mechanically active features for supported types.
- Unsupported features fallback to `TextFeature`.
- Future features (e.g., "Sneak Attack") can be added to the factory.

## Technical Details
- `AttributeBonusFeature` uses `StatBonusEffect` with a duration of -1 (permanent).
- `StandardEffectManager` supports permanent effects (duration < 0).
