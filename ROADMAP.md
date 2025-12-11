# Project Roadmap & Task History

This document tracks the progress of the OpenCombatEngine development, including completed tasks and architectural decisions.

## Strategic Plans
- **[Strategy to Functional Completeness](docs/STRATEGY.md)**: A 7-phase roadmap to transform the core engine into a full game framework.

## Completed Tasks

- [x] **Explore Project Structure**
    - [x] Read README.md
    - [x] Analyze `src` directory structure
    - [x] Identify key components in `Core` and `Implementation`
- [x] Summarize Findings

- [x] **Design Creature Interfaces**
    - [x] Create `ICreature` interface
    - [x] Create `IAbilityScores` interface and Enum
    - [x] Create `IHitPoints` interface
    - [x] Implement basic tests for contracts
- [x] User Review & Commit (Creature Interfaces)

- [x] **Implement Creature Components**
    - [x] Implement `StandardAbilityScores`
    - [x] Implement `StandardHitPoints`
    - [x] Implement `StandardCreature`
    - [x] Add unit tests for implementations
- [x] [Save Architecture Decision Record (Creature Interfaces)](docs/architecture/001-creature-interfaces.md)

- [x] **Cycle 53: Area of Effect Targeting**
    - [x] Define `IShape` and concrete shapes (Sphere, Cone, Cube, Line)
    - [x] Update `IGridManager` to support shape queries
    - [x] Implement shape logic in `StandardGridManager`
- [x] **Cycle 54: AoE Actions & Saving Throws**
    - [x] Add `SavingThrowRolled` event to `ICheckManager`
    - [x] Implement `FireballAction` using `IShape`
- [x] **Cycle 55: Spellcasting System**
    - [x] Implement `StandardSpellCaster`
    - [x] Update `CastSpellAction` to consume slots
    - [x] Create `SpellCastingTests` (JSON)
    - [x] **Refinement**: Implemented `ConditionFactory` for parsing conditions/durations.
- [x] [Save Spellcasting System Refinements ADR](docs/adr/0056-spellcasting-refinements.md)

- [x] **Cycle 56: Open5e Integration**
    - [x] Implement `Open5eClient` and `Open5eAdapter`
    - [x] Create `Open5eContentSource` for Spells/Monsters
    - [x] Verify with Integration Tests
- [x] [Save Open5e Integration ADR](docs/adr/0057-open5e-integration.md)

- [x] **Cycle 57: Combat Loop Refinement**
    - [x] Implement `IWinCondition` Strategy
    - [x] Refactor Turn Loop to skip dead creatures
    - [x] Verify with Combat Loop Tests
- [x] [Save Combat Loop Refinement ADR](docs/adr/0058-combat-loop-refinement.md)

- [x] [Save Combat Loop Refinement ADR](docs/adr/0058-combat-loop-refinement.md)

- [x] **Cycle 58: Combat Serialization**
    - [x] Design State DTOs
    - [x] Implement `IStateful` on Managers
    - [x] Create `CombatSerializer`
    - [x] Verify with Save/Load Tests
- [x] [Save Serialization ADR](docs/adr/0059-combat-serialization.md)

- [x] **Cycle 59: Completeness Verification**
    - [x] Implement End-to-End "Mock Battle" Test
    - [x] Verify Full System Integration

## Future Work
- [ ] Advanced AI Behaviors
- [ ] Networking / Multiplayer Support
- [ ] UI Integration
    - [x] Implement state logic in Standard components
    - [x] Verify with serialization tests (JSON)
- [x] [Save Serialization Architecture Record](docs/architecture/002-creature-serialization.md)

- [x] **Design Action System**
    - [x] Define `ICombatStats` (AC, etc.)
    - [x] Define `IAction` and `ActionResult`
    - [x] Implement `AttackAction`
    - [x] Add `TakeDamage` to `IHitPoints` (mutable)
- [x] [Save Action System Architecture Record](docs/architecture/003-action-system.md)

- [x] **Implement Turn Management**
    - [x] Define `ITurnManager` and `InitiativeRoll`
    - [x] Implement `StandardTurnManager`
    - [x] Add Initiative Tie-Breaker Logic (Dex Score)
    - [x] Verify with TurnManager tests
- [x] [Save Turn Management Architecture Record](docs/architecture/004-turn-management.md)

- [x] **Implement Combat Events**
    - [x] Define Event Arguments (`TurnChangedEventArgs`, `DamageTakenEventArgs`, etc.)
    - [x] Add Events to `ITurnManager` and `IHitPoints`
    - [x] Implement Event Triggering in Standard Components
    - [x] Verify with Event tests
- [x] [Save Combat Events Architecture Record](docs/architecture/005-combat-events.md)

- [x] **Implement Conditions System**
    - [x] Define `ICondition` and `IConditionManager`
    - [x] Add `Conditions` to `ICreature`
    - [x] Implement `StandardConditionManager`
    - [x] Verify with Condition tests
- [x] [Save Conditions System Architecture Record](docs/architecture/006-conditions-system.md)

- [x] **Implement Turn Lifecycle Integration**
    - [x] Add `StartTurn`/`EndTurn` to `ICreature`
    - [x] Implement `StartTurn` in `StandardCreature` (Trigger Tick)
    - [x] Update `StandardTurnManager` to call `StartTurn`
    - [x] Verify with Lifecycle tests
- [x] [Save Turn Lifecycle Architecture Record](docs/architecture/007-turn-lifecycle.md)

- [x] **Implement Action Economy**
    - [x] Define `IActionEconomy`
    - [x] Add `ActionEconomy` to `ICreature`
    - [x] Implement `StandardActionEconomy`
    - [x] Update `StandardCreature.StartTurn` to reset economy
    - [x] Verify with ActionEconomy tests
- [x] [Save Action Economy Architecture Record](docs/architecture/008-action-economy.md)

- [x] **Implement Movement System**
    - [x] Define `IMovement`
    - [x] Add `Movement` to `ICreature`
    - [x] Implement `StandardMovement`
    - [x] Update `StandardCreature.StartTurn` to reset movement
    - [x] Verify with Movement tests
- [x] [Save Movement System Architecture Record](docs/architecture/009-movement-system.md)

- [x] **Unsupervised Cycle 1: Ability Checks & Saving Throws**
    - [x] Implement `RollCheck` and `RollSave`
    - [x] Verify with Tests
- [x] [Save Ability Checks ADR](docs/adr/0010-ability-checks-and-saves.md)

- [x] **Unsupervised Cycle 2: Damage Types & Resistances**
    - [x] Implement `DamageType` Enum
    - [x] Update `TakeDamage` logic
    - [x] Verify with Tests
- [x] [Save Damage Types ADR](docs/adr/0011-damage-types-and-resistances.md)

- [x] **Unsupervised Cycle 3: Death Saving Throws**
    - [x] Implement Death Save logic in `StartTurn`
    - [x] Verify with Tests
- [x] [Save Death Saves ADR](docs/adr/0012-death-saving-throws.md)

- [x] **Unsupervised Cycle 4: Combat Actions Integration**
    - [x] Implement `MoveAction`
    - [x] Update `AttackAction` to consume Action
    - [x] Verify with Tests
- [x] [Save Combat Actions ADR](docs/adr/0013-combat-actions-integration.md)

- [x] **Unsupervised Cycle 5: Concrete Conditions**
    - [x] Implement Standard Conditions (Blinded, etc.)
    - [x] Verify with Tests
- [x] [Save Concrete Conditions ADR](docs/adr/0014-concrete-conditions.md)

- [x] **Unsupervised Cycle 6: Serialization & Cleanup**
    - [x] Update Serialization (CombatStats, Conditions)
    - [x] Fix Warnings & Cleanup
    - [x] Verify with Tests
- [x] [Save Serialization ADR](docs/adr/0015-serialization-and-cleanup.md)

- [x] **Unsupervised Cycle 7: Inventory & Equipment**
    - [x] Implement Items, Weapons, Armor
    - [x] Integrate with CombatStats (AC) and AttackAction
    - [x] Verify with Tests
- [x] [Save Inventory ADR](docs/adr/0016-inventory-and-equipment.md)

- [x] **Unsupervised Cycle 8: Resting System**
    - [x] Implement Short/Long Rest
    - [x] Verify with Tests
- [x] [Save Resting System ADR](docs/adr/0017-resting-system.md)

- [x] **Unsupervised Cycle 9: Spellcasting Foundation**
    - [x] Implement Spells & Spellbook
    - [x] Implement CastSpellAction
    - [x] Verify with Tests
- [x] [Save Spellcasting Foundation ADR](docs/adr/0018-spellcasting-foundation.md)

- [x] **Cycle 10: Content Import System**
    - [x] Implement `IContentImporter`
    - [x] Implement `JsonSpellImporter` (5eTools format)
    - [x] Verify with Tests
- [x] [Save Content Import System Architecture Record](docs/architecture/019-content-import-system.md)

- [x] **Cycle 11: Monster Import System**
    - [x] Implement `JsonMonsterImporter`
    - [x] Implement `MonsterAttackAction`
    - [x] Update `StandardCreature` for custom actions
    - [x] Verify with Tests
- [x] [Save Monster Import System Architecture Record](docs/architecture/020-monster-import-system.md)

- [x] **Cycle 12: Combat Resolution Refactor**
    - [x] Implement "Attack Object" pattern (`AttackResult`)
    - [x] Add `ResolveAttack` to `ICreature`
    - [x] Refactor Actions to use new pipeline
- [x] **Cycle 13: Class Features**
    - [x] Implement `IFeature` and `FeatureManager`
    - [x] Implement `RageFeature` and `SneakAttackFeature`
    - [x] Verify with Tests
- [x] [Save Class Features Architecture Record](docs/architecture/022-class-features.md)

- [x] **Cycle 14: Spellcasting Expansion**
    - [x] Implement Spell Slots & Preparation
    - [x] Implement Spell Save DC & Attack Bonus
    - [x] Verify with Tests
- [x] [Save Spellcasting Expansion Architecture Record](docs/architecture/023-spellcasting-expansion.md)

- [x] **Cycle 15: Spell Resolution**
    - [x] Implement `SpellResolution` model
    - [x] Update `ISpell` and `Spell` for attacks/saves
    - [x] Update `CastSpellAction`
    - [x] Verify with Tests
- [x] [Save Spell Resolution Architecture Record](docs/architecture/024-spell-resolution.md)

- [x] **Cycle 16: Magic Items (Attunement & Passive Bonuses)**
    - [x] Implement `IMagicItem` and `MagicItem`
    - [x] Implement Attunement Logic in `IEquipmentManager`
    - [x] Implement Passive Bonuses (Features/Conditions)
    - [x] Verify with Tests
- [x] [Save Magic Items Architecture Record](docs/architecture/025-magic-items.md)

- [x] **Cycle 17: Content Import Expansion (Magic Items)**
    - [x] Define `MagicItemDto`
    - [x] Implement `JsonMagicItemImporter`
    - [x] Verify with Tests
- [x] [Save Magic Item Import ADR](docs/adr/0026-magic-item-import.md)

- [x] **Cycle 18: Environmental Effects**
    - [x] Implement `Cover` (Enum & Logic)
    - [x] Implement `Obscurement` (Enum & Logic)
    - [x] Update `ResolveAttack` for Cover/Visibility
    - [x] Verify with Tests
- [x] [Save Environmental Effects ADR](docs/adr/0027-environmental-effects.md)

- [x] **Cycle 19: Grid System**
    - [x] Implement `Position` Struct
    - [x] Implement `IGridManager` & `StandardGridManager`
    - [x] Implement Distance Calculation (5e Rules)
    - [x] Verify with Tests
- [x] [Save Grid System ADR](docs/adr/0028-grid-system.md)

- [x] **Cycle 20: Initiative System Refinement**
    - [x] Define `IInitiativeComparer`
    - [x] Implement `StandardInitiativeComparer`
    - [x] Update `StandardTurnManager`
    - [x] Verify with Tests
- [x] [Save Initiative Refinement ADR](docs/adr/0029-initiative-refinement.md)

- [x] **Cycle 21: Movement Validation**
    - [x] Define `IActionContext` & `IActionTarget`
    - [x] Refactor `IAction.Execute`
    - [x] Update `MoveAction`
    - [x] Verify with Tests
- [x] [Save Action Context ADR](docs/adr/0030-action-context.md)

- [x] **Cycle 22: Range Validation**
    - [x] Add `Range` property to Actions
    - [x] Implement Range Validation
    - [x] Verify with Tests
- [x] [Save Range Validation ADR](docs/adr/0031-range-validation.md)

- [x] **Cycle 23: Line of Sight**
    - [x] Implement Obstacles & LOS Algorithm
    - [x] Update Actions to check LOS
    - [x] Verify with Tests
- [x] [Save Line of Sight ADR](docs/adr/0032-line-of-sight.md)

- [x] **Cycle 24: Area of Effect**
    - [x] Define Shapes
    - [x] Implement AOE Targeting
    - [x] Verify with Tests
- [x] [Save Area of Effect ADR](docs/adr/0033-area-of-effect.md)

- [x] **Cycle 25: Movement Cost**
    - [x] Implement Difficult Terrain
    - [x] Update MoveAction for Cost
    - [x] Verify with Tests
- [x] [Save Movement Cost ADR](docs/adr/0034-movement-cost.md)

- [x] **Cycle 26: Flanking**
    - [x] Add `Team` to `ICreature`
    - [x] Implement `IsFlanked` in `GridManager`
    - [x] Update `AttackAction` for Advantage
    - [x] Verify with Tests
- [x] [Save Flanking ADR](docs/adr/0035-flanking.md)

- [x] **Cycle 27: Opportunity Attacks**
    - [x] Implement `GetReach` in `IGridManager`
    - [x] Update `MoveAction` to detect leaving threatened squares
    - [x] Implement `OpportunityAttack` logic
    - [x] Verify with Tests
- [x] [Save Opportunity Attacks ADR](docs/adr/0036-opportunity-attacks.md)

- [x] **Cycle 28: Active Effects System**
    - [x] Define `IActiveEffect` and `IEffectManager`
    - [x] Implement `StandardEffectManager`
    - [x] Integrate with `StandardCreature` and `StandardCombatStats`
    - [x] Implement `StatBonusEffect`
    - [x] Verify with Tests
- [x] [Save Active Effects ADR](docs/adr/0037-active-effects-system.md)

- [x] **Cycle 29: Integrating Active Effects with Rolls**
    - [x] Update `StandardCombatStats` (Initiative)
    - [x] Update `StandardCheckManager` (Checks/Saves)
    - [x] Update `StandardCreature` (Attack/Damage)
    - [x] Update `StandardSpellCaster` (DC/Attack Bonus)
    - [x] Verify with Tests
- [x] [Save Active Effects Integration ADR](docs/adr/0038-active-effects-integration.md)

- [x] **Cycle 30: Pathfinding (A*)**
    - [x] Implement A* Algorithm
    - [x] Handle Difficult Terrain & Obstacles
    - [x] Verify with Tests
- [x] [Save Pathfinding ADR](docs/adr/0039-pathfinding-astar.md)

- [x] **Cycle 31: Spell Import Refinement**
    - [x] Update SpellDto & Importer
    - [x] Verify with Tests
- [x] [Save Spell Import Refinement ADR](docs/adr/0040-spell-import-refinement.md)
- [x] **Cycle 32: Concentration**
    - [x] Implement Concentration Mechanic
    - [x] Update `ISpellCaster` and `CastSpellAction`
    - [x] Implement Constitution Save on Damage
    - [x] Verify with Tests
- [x] [Save Concentration ADR](docs/adr/0041-concentration.md)

- [x] **Cycle 33: Spell Effects (Damage/Healing)**
    - [x] Implement `DamageFormula` and `SaveEffect`
    - [x] Update `ISpell` for damage/healing
    - [x] Update `CastSpellAction` to apply effects
    - [x] Verify with Tests
- [x] [Save Spell Effects ADR](docs/adr/0042-spell-effects.md)

- [x] **Cycle 34: Condition Effects**
- [x] **Cycle 35: Magic Item Passive Bonuses**
- [x] **Cycle 37: Magic Item Active Abilities (Spells)**
    - [x] Implement `ISpellRepository` and `InMemorySpellRepository`
    - [x] Create `CastSpellFromItemAbility`
    - [x] Update `JsonMagicItemImporter` to use repository
    - [x] Verify with Tests
- [x] [Save Magic Item Spells ADR](docs/adr/0044-magic-item-spells.md)
- [x] **Cycle 36: Magic Item Charges & Active Abilities**
    - [x] Implement `RechargeFrequency` and `MagicItemRecharger`
    - [x] Update `IMagicItem` and `MagicItem`
    - [x] Update `JsonMagicItemImporter` for recharge parsing
    - [x] Verify with Tests
- [x] [Save Magic Item Charges ADR](docs/adr/0043-magic-item-charges.md)

- [x] **Cycle 38: Character Classes & Races**
    - [x] Design Class and Race Interfaces
    - [x] Implement Standard Class and Race Models
    - [x] Update `ILevelManager` and `ICreature`
    - [x] Implement Feature Application Logic
    - [x] Verify with Tests
- [x] [Save Character Classes & Races ADR](docs/adr/0045-character-classes-and-races.md)

- [x] **Cycle 39: Class & Race Import**
    - [x] Design Class and Race DTOs
    - [x] Implement `JsonClassImporter`
    - [x] Implement `JsonRaceImporter`
    - [x] Verify with Tests
- [x] [Save Class and Race Import ADR](docs/adr/0046-class-and-race-import.md)

- [x] **Cycle 40: Class & Race Feature Parsing**
    - [x] Implement `FeatureParsingService`
    - [x] Create `TextFeature`
    - [x] Update Importers
    - [x] Verify with Tests
- [x] [Save Feature Parsing ADR](docs/adr/0047-feature-parsing.md)

- [x] **Cycle 41: Feature Factory & Concrete Features**
    - [x] Implement `FeatureFactory`
    - [x] Implement `SenseFeature` & `AttributeBonusFeature`
    - [x] Update `ICreature` with Senses
    - [x] Verify with Tests
- [x] [Save Feature Factory ADR](docs/adr/0048-feature-factory.md)

- [x] **Cycle 42: Resistance, Immunity, & Vulnerability Features**
    - [x] Update `ICombatStats`
    - [x] Implement `DamageAffinityFeature`
    - [x] Update `FeatureFactory`
    - [x] Verify with Tests
- [x] [Save Damage Affinity ADR](docs/adr/0049-resistance-immunity-vulnerability.md)

- [x] **Cycle 43: Action Features**
    - [x] Update `ICreature` with `Actions`
    - [x] Implement `ActionFeature` & `TextAction`
    - [x] Update `FeatureFactory`
    - [x] Verify with Tests
- [x] [Save Action Features ADR](docs/adr/0050-action-features.md)

- [x] **Cycle 44: Spellcasting Features**
    - [x] Update `ISpellCaster` with `UnlearnSpell`
    - [x] Implement `SpellcastingFeature`
    - [x] Update `FeatureFactory` with `ISpellRepository`
    - [x] Verify with Tests
- [x] [Save Spellcasting Features ADR](docs/adr/0051-spellcasting-features.md)

- [x] **Cycle 45: Proficiency Features**
    - [x] Update `ICheckManager`
    - [x] Implement `ProficiencyFeature`
    - [x] Update `FeatureFactory`
    - [x] Verify with Tests
- [x] [Save Proficiency Features ADR](docs/adr/0052-proficiency-features.md)

- [x] **Cycle 46: Class and Race Feature Integration**
    - [x] Update `JsonClassImporter`
    - [x] Verify `JsonRaceImporter`
    - [x] Integration Tests
- [x] [Save Content Import Feature Integration ADR](docs/adr/0053-content-import-feature-integration.md)

- [x] **Cycle 47: Leveling Persistence**
    - [x] Implement `ClassLevelState` and `LevelManagerState`
    - [x] Update `StandardLevelManager` to be `IStateful`
    - [x] Update `StandardCreature` to save/load leveling state
    - [x] Verify with Tests
- [x] [Save Leveling Persistence ADR](docs/adr/0050-leveling-and-experience.md)

- [x] **Cycle 48: Core Event System**
    - [x] Define `MovedEventArgs`, `ConditionEventArgs`, `ActionEventArgs`
    - [x] Add Events to `IMovement`, `IConditionManager`, `ICreature`
    - [x] Implement `StandardMovement`, `StandardConditionManager`, `StandardCreature` events
    - [x] Update `StandardGridManager` to trigger movement events
    - [x] Verify with Tests
- [x] [Save Core Event System ADR](docs/adr/0054-core-event-system.md)

- [x] **Cycle 49: Reaction System Refactor**
    - [x] Add `CreatureMoved` event to `IGridManager`
    - [x] Define `IReaction` and `IReactionManager`
    - [x] Implement `StandardReactionManager` and `OpportunityAttackReaction`
    - [x] Integrate into `StandardCreature`
    - [x] Verify with Tests
- [x] [Save Reaction System ADR](docs/adr/0055-reaction-system.md)

- [x] **Cycle 50: Event-Driven UI (CLI Demo)**
    - [x] Create `OpenCombatEngine.Demo` project
    - [x] Implement `CombatLogger`
    - [x] Verify full event pipeline (Movement -> Reaction -> Action)
    - [x] Document in README.md

- [x] **Cycle 51: Dynamic Item Events**
    - [x] Update `ActionEventArgs` to include `ActionResult`
    - [x] Update `StandardCreature` to publish result
    - [x] Implement `LifeStealingFeature` (Event-Driven)
    - [x] Verify with Tests

- [x] **Cycle 52: Duration-Based Effects**
    - [x] Define `DurationType` Enum (Rounds, Minutes, UntilEndOfTurn)
    - [x] Update `StandardCreature` to call `EndTurn` logic
    - [x] Update `IEffectManager` to handle Turn End expiration
    - [x] Verify with Duration Tests
