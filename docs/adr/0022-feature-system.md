# ADR 022: Feature System

## Status
Accepted

## Context
We needed to implement class features like Barbarian Rage and Rogue Sneak Attack. These features require:
1.  Modifying combat statistics dynamically (Rage adds resistances).
2.  Modifying attacks before they are resolved (Sneak Attack adds damage).
3.  Tracking usage restrictions (Sneak Attack is once per turn).

## Decision
We introduced an `IFeature` interface to encapsulate these behaviors.

### IFeature Interface
```csharp
public interface IFeature
{
    string Name { get; }
    void OnOutgoingAttack(ICreature source, AttackResult attack);
    void OnStartTurn(ICreature creature);
}
```

### Integration
- **StandardCreature**: Maintains a list of `IFeature`.
- **ModifyOutgoingAttack**: `ICreature` exposes `ModifyOutgoingAttack(AttackResult attack)`. `StandardCreature` iterates through features and calls `OnOutgoingAttack`.
- **Turn Lifecycle**: `StandardCreature.StartTurn` calls `OnStartTurn` on all features to reset per-turn limits.

### Dynamic Stats
- **ICombatStats**: Added `AddResistance` and `RemoveResistance` methods.
- **RageCondition**: Implements `ICondition`. On application, it adds resistances. On removal, it removes them.

### Attack Modification
- **AttackResult**: Made `Damage` list mutable (via `AddDamage` method) to allow features to append extra damage dice.
- **SneakAttackFeature**: Checks conditions (Advantage, Weapon Properties) and adds damage to `AttackResult`.

## Consequences
- **Flexibility**: We can now implement many combat features without modifying the core `AttackAction` or `StandardCreature` logic heavily.
- **Complexity**: `AttackResult` is no longer purely immutable, requiring care when passing it around.
- **Performance**: Iterating features on every attack is negligible for typical D&D combat scales.
