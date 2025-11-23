# ADR 024: Spell Resolution Mechanics

## Status
Accepted

## Context
Previously, spells were simple delegates that returned a boolean success/failure. They did not integrate with the combat system (attack rolls, saving throws, damage). We needed a way to resolve these effects mechanically.

## Decision
We introduced a `SpellResolution` model to capture the outcome of a spell cast.
We updated `ISpell` to return `Result<SpellResolution>` instead of `Result<bool>`.
We updated `Spell` (implementation) to be data-driven, accepting properties for:
- `RequiresAttackRoll`
- `SaveAbility`
- `DamageDice`
- `DamageType`

The `Spell.Cast` method now performs the resolution logic:
1.  **Attack Roll**: Uses `IDiceRoller` and caster's `SpellAttackBonus`.
2.  **Saving Throw**: Forces target to roll save against caster's `SpellSaveDC`.
3.  **Damage**: Rolls damage and applies it to target (halved on save if applicable).

We injected `IDiceRoller` into `Spell` via the constructor, which required updating `JsonSpellImporter` and tests.

## Consequences
- **Positive**: Spells now function mechanically in combat. We can define spells like "Fireball" (Save + Damage) or "Fire Bolt" (Attack + Damage) purely via data.
- **Negative**: `Spell` now depends on `IDiceRoller`, making instantiation slightly more complex (handled by DI/Importer).
- **Future**: We need to update the JSON importer to actually parse the new properties from the DTOs. Currently, it defaults to no-op.
