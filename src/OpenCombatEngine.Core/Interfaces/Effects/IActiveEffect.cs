using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Core.Interfaces.Effects
{
    /// <summary>
    /// Defines a temporary effect that can modify a creature's statistics.
    /// </summary>
    public interface IActiveEffect
    {
        /// <summary>
        /// Gets the unique name of the effect.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the effect.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the type of duration logic this effect uses.
        /// </summary>
        DurationType DurationType { get; }

        /// <summary>
        /// Gets the remaining duration in rounds.
        /// -1 indicates a permanent effect.
        /// </summary>
        int DurationRounds { get; }

        /// <summary>
        /// Called when the effect is applied to a creature.
        /// </summary>
        void OnApplied(ICreature target);

        /// <summary>
        /// Called when the effect is removed from a creature.
        /// </summary>
        void OnRemoved(ICreature target);

        /// <summary>
        /// Called at the start of the creature's turn.
        /// Used to decrement duration.
        /// </summary>
        void OnTurnStart(ICreature target);

        /// <summary>
        /// Called at the end of the creature's turn.
        /// Used to expire effects that end at turn end.
        /// </summary>
        void OnTurnEnd(ICreature target);

        /// <summary>
        /// Modifies a specific statistic.
        /// </summary>
        /// <param name="stat">The type of stat being calculated.</param>
        /// <param name="currentValue">The current value of the stat (after previous modifiers).</param>
        /// <param name="skillName">
        /// The named skill this check is for, when <paramref name="stat"/> is
        /// <see cref="StatType.AbilityCheck"/> and the check is skill-based
        /// (e.g. "Intimidation") rather than a bare ability check. Null for
        /// any other stat, or for an ability check with no named skill. An
        /// effect that targets a specific skill (rather than every ability
        /// check) compares this against its own target skill name,
        /// case-insensitively, and only applies when they match.
        /// </param>
        /// <returns>The modified value.</returns>
        int ModifyStat(StatType stat, int currentValue, string? skillName = null);
    }
}
