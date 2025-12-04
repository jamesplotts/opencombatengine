# Cycle 40: Class & Race Feature Parsing

## Goal Description
Expand the Class and Race importers to parse and apply **Features**. Currently, classes and races are imported with empty feature lists. We need to parse the `entries` (for races) and `classTableGroups`/`classFeatures` (for classes) to populate `RacialFeatures` and `FeaturesByLevel`.

## User Review Required
> [!IMPORTANT]
> Feature parsing is complex because features can be simple text, modifiers (like Darkvision), or active abilities.
> For this cycle, we will implement a **Generic Feature** that holds the name and description, and a **FeatureParsingService** that can be extended later to map specific names (e.g., "Sneak Attack") to concrete logic.

## Proposed Changes

### DTOs
#### [MODIFY] [ClassDto](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/Dtos/ClassDto.cs)
- Add `ClassFeature` definitions (often linked via `classFeatures` array in 5e.tools).

#### [MODIFY] [RaceDto](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/Dtos/RaceDto.cs)
- Ensure `Entries` are parsed correctly (often recursive).

### Services
#### [NEW] [FeatureParsingService](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/FeatureParsingService.cs)
- Responsible for converting JSON elements (entries) into `IFeature` instances.
- Will create `TextFeature` (new simple implementation) for now.

### Models
#### [NEW] [TextFeature](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Features/TextFeature.cs)
- Implements `IFeature`.
- Holds `Name`, `Description`.
- `Apply` method does nothing (passive info) or logs.

### Importers
#### [MODIFY] [JsonClassImporter](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/JsonClassImporter.cs)
- Use `FeatureParsingService` to populate `FeaturesByLevel`.

#### [MODIFY] [JsonRaceImporter](file:///home/jamesp/Projects/OpenCombatEngine/src/OpenCombatEngine.Implementation/Content/JsonRaceImporter.cs)
- Use `FeatureParsingService` to populate `RacialFeatures`.

## Verification Plan
### Automated Tests
- **FeatureParsingTests**: Verify that JSON entries are converted to `TextFeature` with correct name/text.
- **ImporterTests**: Update existing tests to verify that imported classes/races now have features in their lists.
