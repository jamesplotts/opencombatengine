# Cycle 39: Class & Race Import

## Goal Description
Implement the ability to import Character Classes and Races from JSON data (specifically 5e.tools format or a simplified version of it). This allows us to populate the `IClassDefinition` and `IRaceDefinition` models created in the previous cycle with actual content.

## User Review Required
> [!NOTE]
> We will be creating DTOs that map to the JSON structure. We'll need to handle parsing of features, which can be complex. For this cycle, we'll focus on basic features (text descriptions) and potentially some structured features if possible (like Ability Score Increases for races).

## Proposed Changes

### DTOs
#### [NEW] [ClassDto](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/Dtos/ClassDto.cs)
- `Name`, `HitDie`, `Proficiencies`, `ClassTableGroups` (for features).

#### [NEW] [RaceDto](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/Dtos/RaceDto.cs)
- `Name`, `Speed`, `Size`, `Ability`, `Entries` (features).

### Importers
#### [NEW] [JsonClassImporter](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/JsonClassImporter.cs)
- Implements `IContentImporter<IClassDefinition>`.
- Parses JSON to `ClassDto` and maps to `ClassDefinition`.

#### [NEW] [JsonRaceImporter](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/JsonRaceImporter.cs)
- Implements `IContentImporter<IRaceDefinition>`.
- Parses JSON to `RaceDto` and maps to `RaceDefinition`.

### Integration
- Ensure `IContentImporter` is generic enough (it is).

## Verification Plan
### Automated Tests
- **JsonClassImporterTests**: Verify importing a sample class JSON.
- **JsonRaceImporterTests**: Verify importing a sample race JSON.
- **Integration**: Verify that imported classes/races can be used by `LevelManager` and `Creature`.
