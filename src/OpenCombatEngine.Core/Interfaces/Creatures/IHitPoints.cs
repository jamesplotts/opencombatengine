using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Models.Events;

namespace OpenCombatEngine.Core.Interfaces.Creatures;

/// <summary>
/// Defines the contract for a creature's hit points and health state.
/// </summary>
public interface IHitPoints
{
    /// <summary>
    /// Gets the current hit points.
    /// </summary>
    int Current { get; }

    /// <summary>
    /// Gets the maximum hit points.
    /// </summary>
    int Max { get; }

    /// <summary>
    /// Gets the temporary hit points.
    /// </summary>
    int Temporary { get; }

    /// <summary>
    /// Gets a value indicating whether the creature is dead.
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// Applies damage to the creature.
    /// </summary>
    /// <param name="amount">Amount of damage to take.</param>
    void TakeDamage(int amount);

    /// <summary>
    /// Applies damage of a specific type to the creature, accounting for resistances/vulnerabilities.
    /// </summary>
    /// <param name="amount">The amount of damage.</param>
    /// <param name="type">The type of damage.</param>
    void TakeDamage(int amount, DamageType type);

    /// <summary>
    /// Heals the creature.
    /// </summary>
    /// <param name="amount">Amount of healing to receive.</param>
    void Heal(int amount);

    event EventHandler<DamageTakenEventArgs> DamageTaken;
    event EventHandler<HealedEventArgs> Healed;
    event EventHandler<DeathEventArgs> Died;
}
