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
        /// Gets whether the creature still has this turn's free object
        /// interaction available — the SRD's "interact with one object for
        /// free" allowance (draw or sheathe a weapon, open a door, and
        /// similar quick, one-handed actions), tracked as its own resource
        /// distinct from Action/BonusAction/Reaction since it's spent far
        /// more often and by different callers (see <see cref="TryUseFreeObjectInteraction"/>).
        /// </summary>
        bool HasFreeObjectInteraction { get; }

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
        /// Consumes this turn's free object interaction if one is still
        /// available and reports whether it succeeded — false means none
        /// remain this turn (see <see cref="GrantFreeObjectInteraction"/> for
        /// the one way a caller can make one available again mid-turn).
        /// Unlike <see cref="UseAction"/>/<see cref="UseBonusAction"/>/
        /// <see cref="UseReaction"/> (which assume the caller already checked
        /// availability), this is check-and-consume in one call, since every
        /// real caller needs exactly that — "spend it if I can, tell me if I
        /// couldn't" — rather than a separate has/use pair.
        /// </summary>
        bool TryUseFreeObjectInteraction();

        /// <summary>
        /// Grants one additional free object interaction this turn, on top
        /// of whatever remains — the hook a future feat plugs into (e.g. a
        /// class feature that lets a creature draw a stowed item without
        /// spending its action): implement <c>IFeature.OnStartTurn</c> and
        /// call this, the same way <c>ActionFeature</c> already grants a
        /// whole extra action via <c>ICreature.AddAction</c>. Never reduces
        /// what's already available — only ever adds.
        /// </summary>
        void GrantFreeObjectInteraction();

        /// <summary>
        /// Resets Action, Bonus Action, and this turn's free object
        /// interaction availability. Typically called at the start of the
        /// creature's turn.
        /// </summary>
        void ResetTurn();

        /// <summary>
        /// Resets Reaction availability.
        /// Typically called at the start of the creature's turn.
        /// </summary>
        void ResetReaction();
    }
}
