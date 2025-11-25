# 44. Magic Item Passive Bonuses

Date: 2025-11-25

## Status

Accepted

## Context

Magic items in 5e often provide passive bonuses to statistics, such as +1 to Armor Class (Ring of Protection), +1 to Saving Throws, or bonuses to Attack/Damage rolls. We needed a way to apply these bonuses automatically when an item is equipped or attuned.

## Decision

We implemented a `StatBonusFeature` class that implements `IFeature`.

1.  **StatBonusFeature**: This feature holds a dictionary of `StatType` to integer bonuses. When applied to a creature, it creates and registers corresponding `StatBonusEffect`s with the creature's `IEffectManager`.
2.  **Lifecycle Hooks**: We updated `IFeature` to include `OnApplied(ICreature)` and `OnRemoved(ICreature)` hooks. This allows features to manage their own effects or state when added/removed from a creature.
3.  **Importing**: The `JsonMagicItemImporter` was updated to parse `bonusAc`, `bonusWeapon`, and `bonusSavingThrow` fields from the JSON data and attach a `StatBonusFeature` to the generated `MagicItem`.
4.  **StandardCreature Integration**: `StandardCreature.AddFeature` and `RemoveFeature` were updated to call the new lifecycle hooks.

## Consequences

*   **Pros**:
    *   Reuses the existing Active Effects system for applying bonuses.
    *   Decouples item logic from creature logic; the item just provides a feature.
    *   Extensible: `StatBonusFeature` can be used for other sources of bonuses (e.g., Feats, Blessings).
*   **Cons**:
    *   Requires `IFeature` implementations to be aware of `IEffectManager` if they want to apply effects.
    *   "Bonus Weapon" handling is slightly simplified; it applies to *all* attacks if implemented as a generic stat bonus, whereas it should technically only apply when using *that specific weapon*. This is a known limitation to be addressed when we refine Weapon-specific features.

## Implementation Details

*   `StatBonusFeature` creates `StatBonusEffect`s with `DurationRounds = -1` (permanent/indefinite).
*   `MagicItemDto` now includes `BonusSavingThrow`.
