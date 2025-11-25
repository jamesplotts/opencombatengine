using OpenCombatEngine.Core.Interfaces.Creatures;

using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Interfaces.Conditions
{
    /// <summary>
    /// Defines a condition (buff/debuff) that can be applied to a creature.
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// Gets the unique name of the condition (e.g., "Blinded").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the condition.
        /// </summary>
        ConditionType Type { get; }

        /// <summary>
        /// Gets the description of the condition's effects.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the remaining duration in rounds.
        /// -1 indicates a permanent condition.
        /// </summary>
        int DurationRounds { get; }

        /// <summary>
        /// Gets the active effects associated with this condition.
        /// </summary>
        System.Collections.Generic.IEnumerable<OpenCombatEngine.Core.Interfaces.Effects.IActiveEffect> Effects { get; }

        /// <summary>
        /// Called when the condition is applied to a creature.
        /// </summary>
        /// <param name="target">The creature receiving the condition.</param>
        void OnApplied(ICreature target);

        /// <summary>
        /// Called when the condition is removed from a creature.
        /// </summary>
        /// <param name="target">The creature losing the condition.</param>
        void OnRemoved(ICreature target);

        /// <summary>
        /// Called at the start of the creature's turn.
        /// Used to decrement duration or trigger periodic effects.
        /// </summary>
        /// <param name="target">The creature with the condition.</param>
        void OnTurnStart(ICreature target);
    }
}
