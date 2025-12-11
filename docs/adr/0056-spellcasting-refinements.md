# 56. Spellcasting System Refinements

Date: 2025-12-11

## Status

Accepted

## Context

The initial implementation of the spellcasting system (`StandardSpellCaster` and `CastSpellAction`) had functional slot consumption but lacked a robust way to handle the application of conditions defined in spell data. `CastSpellAction` contained a temporary, hardcoded helper method for parsing condition strings and creating `Condition` objects. This violated the Single Responsibility Principle and made it difficult to test or extend condition logic without modifying the action itself.

Furthermore, there was ambiguity regarding how "Pact Magic" slots (Warlock style) interacted with standard spell slots, which needed verification.

## Decision

### 1. ConditionFactory
We introduced a static `ConditionFactory` to centralize the creation of `Condition` objects.
- **Responsibilities**:
  - Parsing string durations (e.g., "1 minute", "1 hour") into game rounds.
  - Parsing string names into `ConditionType` enums.
  - Instantiating the appropriate `Condition` class (currently generic, but extensible).
- **Benefits**:
  - Removes parsing logic from `CastSpellAction`.
  - Allows unit testing of parsing logic in isolation (`SpellCastingLogicTests`).
  - Provides a single point of extension if we add complex condition subclasses later.

### 2. Slot Consumption Logic
We verified and solidified the slot consumption strategy in `StandardSpellCaster`:
- **Standard Slots First**: Spells consume slots of their level if available.
- **Pact Magic Fallback**: If no standard slots are available, valid Pact Magic slots (if equal or higher level) are consumed.
- **Upcasting**: Explicit support for consuming higher-level slots is inherent in the design (caller specifies `slotLevel`).

## Consequences

### Positive
- **Testability**: We added `SpellCastingLogicTests` covering edge cases like upcasting and pact magic usage.
- **Clean Code**: `CastSpellAction` is now focused purely on the orchestration of the cast, delegating object creation to the Factory.

### Negative
- **Static Coupling**: Usage of a static Factory introduces a tight coupling, but for simple Data Objects (Values) like Conditions, this is acceptable compared to the overhead of a full DI service for every condition instantiation.

## Compliance
- Adheres to the SOLID principles by separating creation logic (Factory) from execution logic (Action).
