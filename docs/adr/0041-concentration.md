# ADR 041: Concentration

## Status
Accepted

## Context
Many powerful spells in 5e (e.g., *Haste*, *Invisibility*) require Concentration. This mechanic limits a caster to one such spell at a time and introduces a risk of losing the spell when taking damage. Currently, the engine does not track concentration, allowing casters to stack powerful effects or maintain them indefinitely despite taking damage.

## Decision
We will implement the Concentration mechanic with the following rules:
1.  **Tracking**: `ISpellCaster` will track the currently concentrated spell.
2.  **Exclusivity**: Casting a spell that requires concentration immediately ends any existing concentration.
3.  **Damage Check**: Taking damage triggers a Constitution saving throw (DC = max(10, damage / 2)). Failure ends concentration.

## Consequences
-   **Balance**: Spellcasting becomes more balanced and aligned with 5e rules.
-   **Complexity**: `StandardCreature` must now listen for damage events and trigger saves, increasing coupling between components.
-   **UI/Feedback**: The system will need to report when concentration is broken (via event or log).
