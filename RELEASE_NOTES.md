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
- **New**: a `CastSpell` RPC exposes the already-tested `CastSpellAction` (checks `PreparedSpells`, with correct fallback to `KnownSpells` for a non-prepared caster; checks/consumes a slot; rolls the target's real saving throw) over gRPC for the first time — previously reachable only from within the engine itself, never from a downstream consumer like Layforge.
- **Known gap found live, not yet fixed**: `Open5eAdapter.ToStandard(Open5eSpell)` never populates `SpellDto.Damage`/`DamageInflict`/`SpellAttack` — and Open5e's own REST API doesn't expose those as separate structured fields for spells, only as prose inside `desc` (e.g. "3d4 + your spellcasting ability modifier force damage"). Net effect: every spell sourced from this integration currently deals **zero** damage/healing when cast via `CastSpellAction`, regardless of what its real SRD description says — confirmed live via `CastSpell`, not assumed. A real fix needs prose-parsing (or a different/richer data source for damage specifically), not a small mapping tweak; filed here rather than silently worked around.

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
- **`CastSpell` verified live** against the real Open5e API, the gRPC sidecar, and a real downstream consumer (Layforge's Master + a real LLM, `qwen3.8:27b`): a known-but-unprepared spell was hard-rejected by the engine (not just narrated as failing), and a prepared spell with an available slot succeeded — both surfaced correctly through Layforge's `cast_spell` DM tool.
