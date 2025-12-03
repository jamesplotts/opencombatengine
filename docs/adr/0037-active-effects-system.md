# 37. Active Effects System

Date: 2025-11-23

## Status

Accepted

## Context

The engine currently supports permanent passive bonuses (via equipment) and binary conditions (via `IConditionManager`). However, it lacks a unified system for temporary, duration-based effects that modify creature statistics (e.g., *Bless*, *Haste*, *Bane*). We need a way to apply buffs and debuffs that can dynamically alter stats like AC, Speed, Attack Rolls, and Saving Throws.

## Decision

We will implement an **Active Effects System** centered around an `IEffectManager` and `IActiveEffect` interface.

### 1. `IActiveEffect` Interface
Defines a temporary effect.
- **Properties**: `Name`, `Description`, `DurationRounds`.
- **Hooks**: `OnApplied`, `OnRemoved`, `OnTurnStart`.
- **Modification**: `int ModifyStat(StatType stat, int currentValue)` allows the effect to alter a specific stat.

### 2. `StatType` Enum
Standardizes the stats that can be modified.
- Values: `ArmorClass`, `Speed`, `AttackRoll`, `DamageRoll`, `SavingThrow`, `AbilityCheck`, `Initiative`.

### 3. `IEffectManager` Interface
Manages the lifecycle of effects on a creature.
- **Methods**: `AddEffect`, `RemoveEffect`, `Tick`.
- **Aggregation**: `int ApplyStatBonuses(StatType stat, int baseValue)` iterates through active effects to calculate the final value.

### 4. Integration
- `ICreature` will expose an `IEffectManager Effects` property.
- `StandardCombatStats` (and other components) will query `Effects.ApplyStatBonuses` when calculating derived stats (e.g., AC).

## Consequences

### Positive
- **Flexibility**: Allows for a wide range of temporary buffs/debuffs.
- **Consistency**: Unifies stat modification logic.
- **Extensibility**: New effects can be added by implementing `IActiveEffect`.

### Negative
- **Complexity**: Adds another layer of calculation for stats.
- **Performance**: Stat calculation now involves iterating through effects (though the number of effects is expected to be small).

## Alternatives Considered

- **Extending `ICondition`**: We considered adding stat modification to `ICondition`. However, conditions are typically binary states (Blinded, Prone) defined by rules, whereas effects are often variable and cumulative (e.g., multiple sources of bonuses). separating them allows for cleaner separation of concerns.
