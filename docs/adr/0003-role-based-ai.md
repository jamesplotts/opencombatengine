# ADR 0003: Role-Based AI (Tier 3)

## Status
Accepted

## Context
Following the implementation of Tactical AI (Tier 2), we needed to support specialized behaviors for different creature roles (e.g., Artillery, Tank). Open5e data does not strictly define roles, so inference is required.

## Decision
We implemented `RoleBasedAiController` (extending the concepts of `TacticalAiController`) with the following logic:

1. **Role Inference**: 
    - `Open5eAdapter` scans action descriptions for range > 10ft. If found, it adds a `Role:Artillery` tag to the creature.
    - `MonsterMapper` persists these tags to the `StandardCreature`.
2. **Artillery Strategy**:
    - If tagged `Role:Artillery`, the AI prioritizes maintaining a distance of 30-60ft from enemies ("Kiting").
    - It prioritizes actions with "Bow", "Crossbow", "Ray", or "Cast Spell" in their name or type.
3. **Fallback**:
    - If no role-specific logic applies, it falls back to Tier 2 (Tactical) logic (Int-based targeting, Wis-based fleeing) or Tier 1 (Zombie) logic.

## Consequences
- **Positive**: "Artillery" units now behave distinctly, making combat more dynamic (they run away!).
- **Negative**: Role inference is heuristic-based and may be imperfect. Currently only "Artillery" is implemented. "Tank" behavior proved difficult to define without complex aggro mechanics.
- **Future Work**: Implement "Tank" or "Brute" roles if aggro systems or cover systems are expanded.
