# ADR 038: Active Effects Integration

## Status
Accepted

## Context
The Active Effects System (Cycle 28) introduced the ability to modify statistics via `IEffectManager`. Currently, only `ArmorClass` and `Speed` are supported. We need to extend this to cover all major d20 rolls and derived statistics to support a wider range of game features (spells, items, conditions).

## Decision
We will integrate `IEffectManager` into the following components to modify their respective outputs:

1.  **`StandardCombatStats`**:
    -   Apply `StatType.Initiative` to `InitiativeBonus`.

2.  **`StandardCheckManager`**:
    -   Apply `StatType.AbilityCheck` to `RollCheck`.
    -   Apply `StatType.SavingThrow` to `RollSave`.

3.  **`StandardCreature`**:
    -   Apply `StatType.AttackRoll` to outgoing attack rolls.
    -   Apply `StatType.DamageRoll` to outgoing damage rolls.

4.  **`StandardSpellCaster`**:
    -   Apply `StatType.SpellSaveDC` to `SpellSaveDC`.
    -   Apply `StatType.AttackRoll` (or a specific spell attack type if needed, but `AttackRoll` is sufficient for now) to `SpellAttackBonus`.

## Consequences
-   **Flexibility**: The system will support a vast array of D&D 5e effects (e.g., *Bless*, *Bane*, *Guidance*, *Bardic Inspiration*).
-   **Complexity**: Components must now be aware of `IEffectManager` (or have it injected/accessible). `StandardCheckManager` will need access to `Effects`.
-   **Performance**: Slight overhead on every roll to iterate effects, but negligible for typical numbers of active effects.
