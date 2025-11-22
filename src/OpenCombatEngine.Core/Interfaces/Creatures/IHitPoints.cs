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
}
