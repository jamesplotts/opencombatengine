# ADR 018: Spellcasting Foundation

## Status
Accepted

## Context
The system needs to support spellcasting, a major part of 5e.

## Decision
We laid the foundation with `ISpell` and `ISpellCaster`.
- **ISpell**: Represents a spell definition (Name, Level, School).
- **ISpellCaster**: Component holding `KnownSpells` (Spellbook).
- **CastSpellAction**: Action to cast a spell.
- **Logic**: Initial implementation focused on the structure of knowing and casting, without full resolution (saves/attacks) which came later.

## Consequences
- **Positive**: Solid base for the complex spellcasting system.
- **Negative**: Initial version was non-functional for combat (no effects), requiring subsequent cycles to flesh out.
