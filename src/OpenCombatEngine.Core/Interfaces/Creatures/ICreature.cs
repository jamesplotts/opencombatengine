using System;

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
}
