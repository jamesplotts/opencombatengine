# ADR 0050: Action Features

## Status
Accepted

## Context
Features need to be able to grant actions to creatures (e.g., "As an action, you can..."). We need a way to represent these actions and expose them on the creature.

## Decision
We updated `ICreature` to include an `Actions` property and methods to add/remove actions.
We implemented `TextAction` to represent simple, descriptive actions.
We implemented `ActionFeature` to wrap an action and apply it to a creature.
We updated `FeatureFactory` to parse "As an action" and "As a bonus action" phrases.

## Consequences
- `ICreature` interface has changed (breaking change).
- Creatures can now have dynamically added actions from features.
- `StandardCreature` now exposes all actions (movement, unarmed, custom) via the `Actions` property.
