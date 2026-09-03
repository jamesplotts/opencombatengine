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
- **Fixed**: `Open5eAdapter.ToStandard(Open5eSpell)` never populated `SpellDto.Damage`/`DamageInflict`/`SavingThrow` — and Open5e's own REST API doesn't expose those as separate structured fields for spells, only as prose inside `desc` (e.g. "3d4 + your spellcasting ability modifier force damage"). Net effect: every spell sourced from this integration dealt **zero** damage when cast via `CastSpellAction`, regardless of what its real SRD description said — confirmed live via `CastSpell`, not assumed. Fixed with a new `Open5eSpellTextParser` that recovers damage dice/type and saving-throw ability from the SRD's own consistent phrasing ("takes XdY &lt;type&gt; damage", "&lt;ability&gt; saving throw") — best-effort prose parsing, not a full NLP solution, but it covers the standard phrasing every sampled SRD spell (Magic Missile, Fireball, Sacred Flame, Poison Spray, and others) actually uses.
- **Fixed**: three follow-up gaps found alongside the one above, all now closed —
  - **Healing.** `SpellMapper.MapHealingDice` was a permanent stub that always returned `null`, regardless of spell source (not an Open5e-specific bug, but the same class of "the field exists on `ISpell` and nothing ever populates it" gap). `Open5eSpellTextParser` now also extracts a healing spell's dice from "regain[s]/restore[s] ... hit points [equal to XdY]" phrasing (Cure Wounds' "regains ... hit points equal to 1d8 + your spellcasting ability modifier" → `"1d8"`; the ability-modifier suffix is dropped, same simplification damage dice already accept, since `HealingDice` is a fixed string with no caster-dependent bonus).
  - **Attack rolls.** `SpellAttack`/`RequiresAttackRoll` was never populated, and even when set, `CastSpellAction` never branched on it — an attack-roll spell (Ray of Frost, Scorching Ray, Guiding Bolt, Inflict Wounds, Chill Touch) always hit and dealt full damage, the same "auto-hit" bug the save-based fix above closed for save spells. `Open5eSpellTextParser` now detects the SRD's "Make a melee/ranged spell attack" phrasing, and `CastSpellAction` now actually rolls 1d20 + proficiency + casting ability modifier against the target's own armor class (natural 20 always hits, natural 1 always misses, no crit-damage-doubling yet) before applying that instance's damage.
  - **Multi-instance spells.** Magic Missile's three darts, Scorching Ray's three rays — each independently rolled per SRD — previously only ever resolved as a single instance, since the only multiplier `CastSpellAction` had was cantrip level-scaling (Level 0 only). New `ISpell.InstanceCount`/`InstanceCountPerUpcastLevel` members (default 1/0 — no effect on the overwhelming majority of spells) are populated from "you create &lt;count&gt; ... darts/rays/..." and "one more/additional ... for each slot level above" phrasing, and `CastSpellAction`'s damage loop now rolls (and, for an attack-roll spell, independently attack-rolls) each instance separately — a miss on one ray no longer blocks or reduces the others.

### 💾 Persistence & State
- **Full Serialization**: Save and Load combat encounters to JSON.
- **Resilient Restoration**: Win conditions and event subscriptions persist across save/load cycles.

### 🛠️ Core Engine
- **Robust Combat Loop**: Handling of initiative, death skipping, and customizable win conditions.
- **Condition System**: Comprehensive status effect tracking (Blinded, Restrained, etc.) impacting Core mechanics (Advantage/Disadvantage).

## Verification
- **Test Coverage**: 613 Unit and Integration tests passing.
- **End-to-End Verified**: Full "Mock Battle" scenarios run successfully, validating the interaction of all systems.
- **Spellcasting round trip verified live** against the real Open5e API and a real downstream consumer (Layforge's Master + a real LLM): a character with a spell known-but-not-prepared, and another prepared, correctly distinguished the two through the gRPC sidecar.
- **`CastSpell` verified live** against the real Open5e API, the gRPC sidecar, and a real downstream consumer (Layforge's Master + a real LLM, `qwen3.8:27b`): a known-but-unprepared spell was hard-rejected by the engine (not just narrated as failing), and a prepared spell with an available slot succeeded — both surfaced correctly through Layforge's `cast_spell` DM tool.
- **Damage/healing/multi-instance fixes verified live**, same stack: a prepared Magic Missile cast dealt 8 damage (three independently-rolled 1d4+1 darts, consistent with a real 3-instance roll rather than one), and a prepared Cure Wounds cast raised the caster from 10/18 to 16/18 HP (a real 1d8 roll, persisted). Attack-roll gating (Ray of Frost/Scorching Ray-style) is covered by 9 new deterministic-dice unit tests rather than a live run — a live LLM session can't control which d20 a hit/miss test needs, so this one is unit-only, not silently claimed as live-verified.
