# ADR 040: Spell Import Refinement

## Status
Accepted

## Context
The current `JsonSpellImporter` only maps basic spell properties (Name, Level, School, etc.). It ignores critical combat data such as:
-   Attack Rolls (`spellAttack`)
-   Saving Throws (`savingThrow`)
-   Damage (`damageInflict`, `damage`)
-   Area of Effect

Without this data, imported spells cannot be used in the combat resolution system.

## Decision
We will update `SpellDto` and `JsonSpellImporter` to map these fields from the 5e.tools JSON format.

### Mapping Logic
-   **RequiresAttackRoll**: True if `spellAttack` array exists and contains "M" (Melee) or "R" (Ranged).
-   **SaveAbility**: Mapped from the first entry in `savingThrow` array (e.g., "dex" -> `Ability.Dexterity`).
-   **DamageType**: Mapped from the first entry in `damageInflict` array.
-   **DamageDice**: Extracted from the `damage` array (e.g., `[["8d6"]]` -> "8d6").

## Consequences
-   **Usability**: Imported spells will now function correctly in combat.
-   **Maintenance**: The importer becomes more coupled to the specific 5e.tools JSON structure, which may change.
-   **Complexity**: Parsing logic increases slightly.
