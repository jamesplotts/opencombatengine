# 45. Magic Item Charges & Active Abilities

Date: 2025-11-25

## Status

Accepted

## Context

Many magic items in 5e (like Wands and Staffs) have a pool of charges that can be expended to produce effects or cast spells. We needed a standard way to track these charges and allow creatures to use them.

## Decision

We updated `IMagicItem` to include charge management properties and methods:

1.  **Charge Tracking**: Added `Charges`, `MaxCharges`, and `RechargeRate` properties to `IMagicItem`.
2.  **Consumption/Recharge**: Added `ConsumeCharges(int amount)` and `Recharge(int amount)` methods to handle state changes safely.
3.  **CastSpellFromItemAction**: Created a new action class that inherits from `CastSpellAction`.
    *   It overrides `ConsumeResources` to consume item charges instead of spell slots.
    *   It overrides `CheckPreparation` to bypass spell preparation requirements (as items don't require preparation).
4.  **Refactoring CastSpellAction**: We refactored `CastSpellAction` to make `ConsumeResources` and `CheckPreparation` virtual, allowing for this extension without code duplication.

## Consequences

*   **Pros**:
    *   Standardized interface for all charge-based items.
    *   Reuses existing spellcasting logic via inheritance.
    *   Flexible: `UseItemAction` (generic) can be implemented similarly using `ConsumeCharges`.
*   **Cons**:
    *   `CastSpellFromItemAction` relies on `CastSpellAction` internals; changes to the base class must be careful not to break the item version.
    *   Recharge logic (e.g., "1d6+1 at dawn") is currently just a string; actual automated recharging is not yet implemented (requires a "Rest" or "Time" system trigger).

## Implementation Details

*   `MagicItemDto` now parses `charges` and `recharge` fields.
*   `MagicItem` constructor initializes charges to `MaxCharges`.
