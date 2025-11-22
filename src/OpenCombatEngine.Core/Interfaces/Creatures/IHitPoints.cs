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

    /// <summary>
    /// Gets the number of successful death saving throws.
    /// </summary>
    int DeathSaveSuccesses { get; }

    /// <summary>
    /// Gets the number of failed death saving throws.
    /// </summary>
    int DeathSaveFailures { get; }

    /// <summary>
    /// Gets whether the creature is stable (at 0 HP but not dying).
    /// </summary>
    bool IsStable { get; }

    /// <summary>
    /// Records the result of a death saving throw.
    /// </summary>
    /// <param name="success">Whether the save was successful.</param>
    /// <param name="critical">Whether the result was critical (Nat 1 or Nat 20 effects).</param>
    void RecordDeathSave(bool success, bool critical = false);

    /// <summary>
    /// Stabilizes the creature, resetting death saves and stopping the dying process.
    /// </summary>
    void Stabilize();

    event EventHandler<DamageTakenEventArgs> DamageTaken;
    event EventHandler<HealedEventArgs> Healed;
    event EventHandler<DeathEventArgs> Died;
}
