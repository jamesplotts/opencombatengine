using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Core.Interfaces.Creatures;

/// <summary>
/// Defines the core contract for any creature in the system.
/// </summary>
public interface ICreature
{
    /// <summary>
    /// Gets the unique identifier for this creature instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the creature.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the team the creature belongs to.
    /// </summary>
    string Team { get; } // e.g. "Player", "Monster"

    /// <summary>
    /// Gets the ability scores for the creature.
    /// </summary>
    IAbilityScores AbilityScores { get; }

    /// <summary>
    /// Gets the hit points for the creature.
    /// </summary>
    IHitPoints HitPoints { get; }

    /// <summary>
    /// Gets the combat statistics for the creature.
    /// </summary>
    ICombatStats CombatStats { get; }

    /// <summary>
    /// Gets the condition manager for the creature.
    /// </summary>
    IConditionManager Conditions { get; }

    /// <summary>
    /// Called at the start of the creature's turn.
    /// </summary>
    void StartTurn();

    /// <summary>
    /// Called at the end of the creature's turn.
    /// </summary>
    void EndTurn();

    /// <summary>
    /// Gets the action economy manager for the creature.
    /// </summary>
    IActionEconomy ActionEconomy { get; }

    /// <summary>
    /// Gets the encumbrance level of the creature.
    /// </summary>
    OpenCombatEngine.Core.Enums.EncumbranceLevel EncumbranceLevel { get; }

    /// <summary>
    /// Gets the active effects manager for the creature.
    /// </summary>
    OpenCombatEngine.Core.Interfaces.Effects.IEffectManager Effects { get; }

    /// <summary>
    /// Gets the movement manager for the creature.
    /// </summary>
    IMovement Movement { get; }

    /// <summary>
    /// Gets the check manager for ability checks and saves.
    /// </summary>
    ICheckManager Checks { get; }

    /// <summary>
    /// Gets the creature's proficiency bonus.
    /// </summary>
    int ProficiencyBonus { get; }

    /// <summary>
    /// Gets the inventory.
    /// </summary>
    IInventory Inventory { get; }

    /// <summary>
    /// Gets the equipment manager.
    /// </summary>
    IEquipmentManager Equipment { get; }

    /// <summary>
    /// Gets the spellcasting component, if the creature is a spellcaster.
    /// </summary>
    ISpellCaster? Spellcasting { get; }

    /// <summary>
    /// Resolves an incoming attack.
    /// </summary>
    /// <param name="attack">The attack data.</param>
    /// <returns>The outcome of the resolution.</returns>
    AttackOutcome ResolveAttack(AttackResult attack);

    /// <summary>
    /// Modifies an outgoing attack before it is resolved.
    /// </summary>
    /// <param name="attack">The attack data.</param>
    void ModifyOutgoingAttack(AttackResult attack);

    /// <summary>
    /// Performs a rest.
    /// </summary>
    /// <param name="type">The type of rest.</param>
    /// <param name="hitDiceToSpend">Number of hit dice to spend (Short Rest only).</param>
    void Rest(RestType type, int hitDiceToSpend = 0);

    /// <summary>
    /// Gets the level manager for the creature.
    /// </summary>
    ILevelManager LevelManager { get; }

    /// <summary>
    /// Adds a feature to the creature.
    /// </summary>
    /// <param name="feature">The feature to add.</param>
    void AddFeature(OpenCombatEngine.Core.Interfaces.Features.IFeature feature);

    /// <summary>
    /// Removes a feature from the creature.
    /// </summary>
    /// <param name="feature">The feature to remove.</param>
    void RemoveFeature(OpenCombatEngine.Core.Interfaces.Features.IFeature feature);

    /// <summary>
    /// Gets the race definition of the creature.
    /// </summary>
    OpenCombatEngine.Core.Interfaces.Races.IRaceDefinition? Race { get; }

    /// <summary>
    /// Gets the senses of the creature (e.g. Darkvision -> 60).
    /// </summary>
    System.Collections.Generic.IDictionary<string, int> Senses { get; }

    /// <summary>
    /// Gets the list of actions available to the creature.
    /// </summary>
    System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Actions.IAction> Actions { get; }

    /// <summary>
    /// Adds an action to the creature.
    /// </summary>
    /// <param name="action">The action to add.</param>
    void AddAction(OpenCombatEngine.Core.Interfaces.Actions.IAction action);

    /// <summary>
    /// Removes an action from the creature.
    /// </summary>
    /// <param name="action">The action to remove.</param>
    void RemoveAction(OpenCombatEngine.Core.Interfaces.Actions.IAction action);
}
