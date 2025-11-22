# Project Roadmap & Task History

This document tracks the progress of the OpenCombatEngine development, including completed tasks and architectural decisions.

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

- [x] **Design Creature Serialization**
    - [x] Create State DTOs (`CreatureState`, `AbilityScoreState`, etc.)
    - [x] Add `ToState()`/`FromState()` to component interfaces
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
