# 51. Magic Item Charges and Active Abilities

Date: 2025-11-26

## Status

Accepted

## Context

Magic items often have charges (e.g., Wand of Magic Missiles) that are consumed to activate abilities. Some items also have active abilities that don't require charges but are distinct actions (e.g., a sword that can shed light on command). We need a standard way to model these capabilities.

## Decision

We will extend `IMagicItem` and introduce `IMagicItemAbility`.

### 1. IMagicItem Extensions
- `MaxCharges`: Total charges the item can hold.
- `CurrentCharges`: Current available charges.
- `Recharge(int amount)`: Method to restore charges.
- `ConsumeCharges(int amount)`: Method to spend charges.
- `Abilities`: A collection of `IMagicItemAbility`.

### 2. IMagicItemAbility
This interface represents a specific power of the item.
- `Name`: Name of the ability.
- `Cost`: Charge cost (0 for free abilities).
- `ActionType`: The type of action required (Action, BonusAction, etc.).
- `Execute(ICreature user, IActionContext context)`: The logic to run when used.

### 3. UseMagicItemAction
A generic `IAction` implementation that wraps an `IMagicItemAbility`.
- When executed, it validates charges.
- Consumes charges.
- Delegates execution to the `IMagicItemAbility`.

## Consequences

- **Flexibility**: Items can have multiple abilities with different costs.
- **Standardization**: All active item powers use the same action framework.
- **State Management**: `MagicItem` becomes stateful (charges), requiring serialization updates (handled in previous cycles or to be added).

## Alternatives Considered

- **Hardcoding Item Actions**: Creating specific `Action` classes for every item (e.g., `WandOfMagicMissilesAction`). This scales poorly.
- **Effects Only**: Treating everything as passive effects. Doesn't handle active triggers well.
