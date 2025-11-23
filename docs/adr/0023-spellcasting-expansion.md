# ADR 023: Spellcasting System Expansion

## Status
Accepted

## Context
The initial spellcasting system (Cycle 9) provided basic spell execution but lacked resource management (Spell Slots) and preparation mechanics. We also needed to integrate spellcasting with combat statistics (Spell Save DC, Spell Attack Bonus).

## Decision
We expanded the `ISpellCaster` interface and `StandardSpellCaster` implementation to support these features.

### Spell Slots
- **SpellSlotManager**: Created a dedicated component to manage max and current slots per level.
- **ISpellCaster**: Added `HasSlot`, `GetSlots`, `GetMaxSlots`, `ConsumeSlot`, `RestoreAllSlots`, and `SetSlots`.
- **StandardSpellCaster**: Delegates slot management to `SpellSlotManager`.

### Preparation
- **ISpellCaster**: Added `PreparedSpells` property, `PrepareSpell` and `UnprepareSpell` methods.
- **Preparation Mode**: `StandardSpellCaster` now supports a `isPreparedCaster` flag.
    - If `true` (e.g. Wizard), `PreparedSpells` returns only explicitly prepared spells.
    - If `false` (e.g. Sorcerer), `PreparedSpells` returns `KnownSpells`.
- **CastSpellAction**: Updated to verify that a spell is in `PreparedSpells` before casting.

### Combat Statistics
- **ISpellCaster**: Added `SpellSaveDC` and `SpellAttackBonus` properties.
- **Calculation**: `StandardSpellCaster` calculates these based on `IAbilityScores`, `ProficiencyBonus`, and `SpellcastingAbility`.
    - DC = 8 + Proficiency + AbilityMod
    - Attack = Proficiency + AbilityMod

## Consequences
- **Resource Management**: Spells now correctly consume resources, allowing for long rest restoration.
- **Flexibility**: The system supports both "Known" and "Prepared" casters via a simple flag.
- **Combat Integration**: Spells can now use the caster's stats for DC and attack rolls (though individual spell implementations need to use these values).
