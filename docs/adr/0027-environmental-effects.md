# ADR 027: Environmental Effects

## Status
Accepted

## Context
Combat is often influenced by the environment, specifically Cover (walls, obstacles) and Obscurement (fog, darkness).

## Decision
We will model these effects as context provided during an attack resolution.
- **Enums**: `CoverType` (None, Half, ThreeQuarters, Total) and `ObscurementType` (None, Lightly, Heavily).
- **AttackResult**: Updated to include `TargetCover` and `TargetObscurement` properties.
- **Resolution**: `ResolveAttack` in `StandardCreature` will apply:
    - **Cover**: AC bonuses (+2 for Half, +5 for Three-Quarters). Total cover prevents the attack (returns failure).
    - **Obscurement**: 
        - **Heavily Obscured**: Attack is made with Disadvantage (attacker cannot see target).
        - **Lightly Obscured**: No direct effect on Attack Roll (affects Perception, which is separate).

## Consequences
- **Positive**: Mechanizes important tactical elements of 5e.
- **Negative**: Requires the caller (Action or Game Loop) to determine the cover/obscurement state before creating the `AttackResult`. This separation is intentional (geometry vs mechanics).
