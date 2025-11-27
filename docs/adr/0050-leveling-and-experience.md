# 50. Leveling and Experience System

Date: 2025-11-26

## Status

Accepted

## Context

The system needs to track character progression, including Experience Points (XP), Levels, and Proficiency Bonus. We also need to support multiclassing (e.g., a character with 3 levels in Fighter and 1 in Rogue).

## Decision

We will introduce a new component `ILevelManager` to handle all progression logic.

### 1. ILevelManager Interface
This interface will expose:
- `TotalLevel`: The sum of all class levels.
- `ExperiencePoints`: Current XP.
- `ProficiencyBonus`: Calculated based on `TotalLevel`.
- `Classes`: A dictionary or list of class names and their levels.
- `LevelUp(string className, int hitDieAverage, bool takeAverageHp)`: Method to increase level.
- `AddExperience(int amount)`: Method to add XP.

### 2. StandardLevelManager
The implementation will:
- Maintain a `Dictionary<string, int>` for class levels.
- Calculate `ProficiencyBonus` using the standard 5e formula: `ceil(TotalLevel / 4) + 1` (or `(TotalLevel - 1) / 4 + 2`).
- On `LevelUp`:
    - Increment the class level.
    - Calculate HP increase (Average or Rolled + Con Mod).
    - Update `IHitPoints.Max` and `Current`.
    - Add a Hit Die to `IHitPoints`.

### 3. Integration
- `ICreature` will have a `LevelManager` property.
- `StandardCreature` will initialize `StandardLevelManager`.
- `ProficiencyBonus` on `ICreature` will delegate to `LevelManager.ProficiencyBonus`.

## Consequences

- **Separation of Concerns**: Progression logic is isolated from the main creature class.
- **Multiclassing Support**: The dictionary approach inherently supports multiclassing.
- **Dependency**: `StandardLevelManager` will need access to `IHitPoints` and `IAbilityScores` (for Con mod) to handle HP increases on level up.

## Alternatives Considered

- **Tracking Level on ICreature directly**: Would clutter `ICreature` and make multiclassing logic harder to manage.
- **Separate Class Objects**: We could have complex `Class` objects, but for now, string identifiers and simple level tracking are sufficient.
