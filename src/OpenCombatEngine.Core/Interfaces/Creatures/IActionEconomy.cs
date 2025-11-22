namespace OpenCombatEngine.Core.Interfaces.Creatures
{
    /// <summary>
    /// Tracks the available action resources for a creature (Action, Bonus Action, Reaction).
    /// </summary>
    public interface IActionEconomy
    {
        /// <summary>
        /// Gets whether the creature has an Action available.
        /// </summary>
        bool HasAction { get; }

        /// <summary>
        /// Gets whether the creature has a Bonus Action available.
        /// </summary>
        bool HasBonusAction { get; }

        /// <summary>
        /// Gets whether the creature has a Reaction available.
        /// </summary>
        bool HasReaction { get; }

        /// <summary>
        /// Consumes the creature's Action.
        /// </summary>
        void UseAction();

        /// <summary>
        /// Consumes the creature's Bonus Action.
        /// </summary>
        void UseBonusAction();

        /// <summary>
        /// Consumes the creature's Reaction.
        /// </summary>
        void UseReaction();

        /// <summary>
        /// Resets Action and Bonus Action availability.
        /// Typically called at the start of the creature's turn.
        /// </summary>
        void ResetTurn();

        /// <summary>
        /// Resets Reaction availability.
        /// Typically called at the start of the creature's turn.
        /// </summary>
        void ResetReaction();
    }
}
