# Release Candidate 1 (v0.6.0-rc1)

This release marks a significant milestone for the **OpenCombatEngine**, introducing advanced AI behaviors, loot generation, and full Open5e content integration.

## Key Features

### 🧠 Advanced AI System
- **Tier 1 (Zombie)**: Basic "Move & Attack" logic for simple creatures.
- **Tier 2 (Tactical)**: Smart targeting (focus on weak/low HP enemies) and self-preservation logic.
- **Tier 3 (Role-Based)**: Specialized behaviors like "Artillery" (Kiting/Ranged superiority).

### ⚔️ Item Library & Loot
- **Standard Item Library**: Integration with Open5e to fetch Weapons, Armor, and Magic Items.
- **Procedural Loot Generation**: Generate CR-appropriate treasure bundles including Gold/Silver/Copper and Items based on rarity probabilities.
- **Unified Interfaces**: `IItem`, `IWeapon`, `IArmor` standardized across the engine.

### 🌐 Open5e Integration
- **Direct API Access**: Fetch Spells, Monsters, and Items directly from `api.open5e.com`.
- **Seamless Mapping**: Automatic conversion of Open5e JSON data to Engine DTOs.

### 💾 Persistence & State
- **Full Serialization**: Save and Load combat encounters to JSON.
- **Resilient Restoration**: Win conditions and event subscriptions persist across save/load cycles.

### 🛠️ Core Engine
- **Robust Combat Loop**: Handling of initiative, death skipping, and customizable win conditions.
- **Condition System**: Comprehensive status effect tracking (Blinded, Restrained, etc.) impacting Core mechanics (Advantage/Disadvantage).

## Verification
- **Test Coverage**: 480+ Unit and Integration tests passing.
- **End-to-End Verified**: Full "Mock Battle" scenarios run successfully, validating the interaction of all systems.
