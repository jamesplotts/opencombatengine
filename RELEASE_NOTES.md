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
- **Fixed since rc1**: the bulk *list* fetchers (`GetAllWeaponsAsync`/`GetAllArmorAsync`/`GetAllMagicItemsAsync`/the new `GetAllSpellsAsync`) share one generic `Open5eListResult<T>` type whose `Results` property was get-only — `System.Text.Json` silently left it empty against a real API response (scalar fields like `count`/`next` deserialized fine; only the results array was affected), with no exception thrown. Every prior test of this path used a mock constructed directly in C#, which never exercises real deserialization, so this went unnoticed. Found live while wiring the gRPC sidecar's spell repository; the fix (a settable property) applies to all four content types, not just spells.
- **New**: the gRPC sidecar (`OpenCombatEngine.GrpcSidecar`) now populates a real `ISpellRepository` from this integration at startup, so `spellcasting` (known/prepared spells, slots) actually survives the gRPC round trip — previously always `null` regardless of what was sent in, for every creature, on every RPC that reconstructed one.

### 💾 Persistence & State
- **Full Serialization**: Save and Load combat encounters to JSON.
- **Resilient Restoration**: Win conditions and event subscriptions persist across save/load cycles.

### 🛠️ Core Engine
- **Robust Combat Loop**: Handling of initiative, death skipping, and customizable win conditions.
- **Condition System**: Comprehensive status effect tracking (Blinded, Restrained, etc.) impacting Core mechanics (Advantage/Disadvantage).

## Verification
- **Test Coverage**: 558 Unit and Integration tests passing.
- **End-to-End Verified**: Full "Mock Battle" scenarios run successfully, validating the interaction of all systems.
- **Spellcasting round trip verified live** against the real Open5e API and a real downstream consumer (Layforge's Master + a real LLM): a character with a spell known-but-not-prepared, and another prepared, correctly distinguished the two through the gRPC sidecar.
