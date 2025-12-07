# Strategy to Functional Completeness

This document maps out the strategy to take the OpenCombatEngine from a "Rules Engine" to a "Functionally Complete Framework" that a developer can build a game upon. The focus is on **Separation of Concerns**, ensuring the Core handles Logic and State, while exposing Hooks for UI (Visuals) and AI (Behavior).

## Core Philosophy
1.  **Event-Driven UI**: The Core never calls the UI. The Core emits **Events**, and the UI listens.
2.  **Controller-Driven Logic**: The Core never decides "What to do". It asks an `IController` (which could be a Human Input layer or an AI script).
3.  **Data-Driven Effects**: Complex logic (Fireball, Magic Missile) should be defined in data (JSON), not hardcoded C# classes, to allow easy content expansion.

## Roadmap to Completion

### Phase 1: The Nervous System (Events)
**Goal**: Allow the UI to "see" everything without polling.
*   **Gap**: Currently, `Damage` and `Turn` have events, but `Movement`, `Conditions`, `Action Start/End`, and `Save Results` are partially silent.
*   **Solution**: Implement a centralized `CombatEventBus` or enrich interfaces with standard C# Events.
*   **Key Cycles**:
    *   **Cycle 48: Core Event System**: Define generic events (`EntityMoved`, `ActionStarted`, `ConditionApplied`) and implement a Bus or Observable pattern.

### Phase 2: The Brain (Controllers)
**Goal**: Remove hard assumptions about who is playing.
*   **Gap**: Currently, you call `MoveAction.Execute()`. In a real game, the Engine needs to *ask* the active actor "What do you want to do?".
*   **Solution**: Introduce `IController`.
    *   `PlayerController`: Pipes input from UI.
    *   `AIController`: Pipes input from Behavior Trees/GOAP.
*   **Key Cycles**:
    *   **Cycle 49: Controller Interface**: Refactor `ITurnManager` to request actions from `IController`.

### Phase 3: The Reflexes (Reactions)
**Goal**: Implement the "Interrupt" mechanic critical to 5e (Shield, Counterspell, Opportunity Attacks).
*   **Gap**: The current engine runs linearly. Logic for "Opportunity Attack" is hardcoded. We need a generic "Pause and Ask" system.
*   **Solution**: A "Reaction Window" system. When an event occurs (e.g., "Movement leaving square"), the engine pauses resolution, queries all systems for "Triggers", asks Controllers for "Reaction", then resumes.
*   **Key Cycles**:
    *   **Cycle 50: Reaction System**: Implement the Interrupt/Trigger flow.

### Phase 4: The Instructions (Scripting)
**Goal**: Eliminate custom C# classes for every spell.
*   **Gap**: `Fireball` currently requires a specific C# Action to define "Select Sphere at Range X, apply Dex Save, deal 8d6 Fire".
*   **Solution**: A robust Effect Definition system (JSON) allowing composition of primitives.
    *   *Example JSON*: `{ "Target": "Sphere(20)", "Save": "Dex", "OnFail": "Damage(Fire, 8d6)", "OnSuccess": "Half" }`
*   **Key Cycles**:
    *   **Cycle 51: Advanced Effect Scripting**: Implement a parser/processor for effect primitives.
    *   **Cycle 52: Complex Spell Templates**: Convert existing hardcoded spells to this new standard.

### Phase 5: The World (Environment)
**Goal**: Standardize non-creature interactions.
*   **Gap**: No standard way to open a door, trigger a trap, or loot a chest.
*   **Solution**: `IInteractable` interface and `InteractAction`.
*   **Key Cycles**:
    *   **Cycle 53: Environmental Interaction**: Doors, Containers, Switches.

### Phase 6: The Memory (Serialization V2)
**Goal**: Full "Save Game" capability.
*   **Gap**: We can serialize a Creature, but not the "Battle" (Turn Order, Initiative, Active Projectiles, Positions).
*   **Solution**: `GameState` serialization that wraps everything.
*   **Key Cycles**:
    *   **Cycle 54: Full State Serialization**: Snapshotting the entire Combat Engine.

## Summary Checklist
- [ ] **Cycle 48**: Core Event System (UI Hooks)
- [ ] **Cycle 49**: Controller Interface (AI Hooks)
- [ ] **Cycle 50**: Reaction System (Reflexes)
- [ ] **Cycle 51**: Advanced Effect Scripting (Data-Driven Logic)
- [ ] **Cycle 52**: Complex Spell Templates
- [ ] **Cycle 53**: Environmental Interaction
- [ ] **Cycle 54**: Full State Serialization
