# ADR 017: Resting System

## Status
Accepted

## Context
Creatures need to recover hit points and resources via Short and Long Rests.

## Decision
We implemented a `Rest` method on `ICreature`.
- **Enum**: `RestType` (Short, Long).
- **Logic**: 
    - **Short Rest**: Allows spending Hit Dice to heal.
    - **Long Rest**: Fully restores HP and half max Hit Dice.
- **Integration**: Components (like Spellcasting) will hook into this to restore their specific resources (slots).

## Consequences
- **Positive**: Standardized rest mechanic.
- **Negative**: Component-specific rest logic (like resetting Action Surge) needs to be manually wired up or event-driven (currently manual).
