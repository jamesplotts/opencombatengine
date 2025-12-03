# 17. Resting System

Date: 2025-11-22

## Status

Accepted

## Context

Creatures need to recover resources (HP, Hit Dice, Spell Slots) via Short and Long Rests.

## Decision

We implemented the `Rest` method on `ICreature` and `RestType` enum.

- **Short Rest**:
    - Optional: Spend Hit Dice to regain HP.
    - Logic: Roll Hit Die + Con Mod for each die spent.
    - Resets: Short-rest resources (e.g. Warlock slots, though not yet implemented).
- **Long Rest**:
    - Regain full HP.
    - Regain half of max Hit Dice (minimum 1).
    - Resets: Long-rest resources (Spell Slots, Abilities).
    - Reduces Exhaustion (future).
- **Hit Dice**: `IHitPoints` now tracks `HitDiceTotal` and `HitDiceRemaining`.

## Consequences

- Resource management loop is closed (Expend -> Rest -> Recover).
- Hit Dice mechanic adds strategic depth to healing.
