# 18. Spellcasting Foundation

Date: 2025-11-22

## Status

Accepted

## Context

Magic is a pillar of fantasy combat. We needed a system to define spells, manage slots, and cast them.

## Decision

We implemented `ISpell`, `ISpellCaster`, and `CastSpellAction`.

- **ISpell**: Defines spell data (Level, School, Range, etc.) and an effect delegate `Cast(caster, target)`.
- **ISpellCaster**: Component attached to `ICreature` (via `Spellcasting` property).
    - Tracks Spell Slots (Current/Max per level).
    - Tracks Known/Prepared Spells.
- **CastSpellAction**:
    - Validates caster has the spell and a slot.
    - Consumes the slot.
    - Invokes `ISpell.Cast()`.
- **SpellSchool**: Enum for the 8 schools of magic.

## Consequences

- Creatures can now be spellcasters.
- Spells are code-defined (for now), allowing complex logic in delegates.
- Foundation is set for implementing specific spells and class-based casting rules.
