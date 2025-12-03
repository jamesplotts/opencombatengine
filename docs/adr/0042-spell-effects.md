# ADR 042: Spell Effects Implementation

## Status
Accepted

## Context
We have implemented spell casting (slot consumption) and concentration, but spells do not yet apply their primary effects: Damage and Healing. We need a standardized way to define and apply these effects.

## Decision
We will expand the `ISpell` interface and `CastSpellAction` to handle damage and healing.

1.  **ISpell Interface**:
    *   `IReadOnlyList<DamageFormula> Damage { get; }`: A list of damage formulas (Dice + Type).
    *   `string? HealingDice { get; }`: Dice formula for healing (e.g., "1d8").
    *   `SaveEffect SaveEffect { get; }`: Enum defining effect of a successful save (None, HalfDamage, Negate).

2.  **CastSpellAction**:
    *   Will be responsible for rolling the dice.
    *   Will iterate over targets (from `ActionContext`).
    *   Will trigger saving throws on targets if required.
    *   Will calculate final damage based on save result.
    *   Will call `target.HitPoints.TakeDamage` or `target.HitPoints.Heal`.

3.  **5e.tools Data**:
    *   We will parse the `damage` array for damage types.
    *   We will parse `healing` entries (often in `meta` or inferred from description/school, but 5e.tools usually has specific fields for scaling). *Correction*: 5e.tools often puts healing in `entries` or specific `scaling` objects. We might need to look for specific tags or just parse the `damage` field which sometimes contains healing in other schemas, but for 5e.tools it's often distinct. We will start with standard `damage` parsing and look for `healing` property if it exists in the JSON, or infer from `entries` if needed.

## Consequences
*   **Positive**: Spells will finally have mechanical impact on HP.
*   **Negative**: Parsing 5e.tools healing data might be complex as it's less standardized than damage.
*   **Risks**: Complex spells (e.g. "Ice Storm" doing bludgeoning + cold) need careful parsing.

## Technical Notes
*   `DamageFormula` struct/class: `{ string Dice, DamageType Type }`.
*   `SaveEffect` enum: `None`, `Half`, `Negate`.
