using System;
using OpenCombatEngine.Core.Interfaces.Conditions;

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
}
