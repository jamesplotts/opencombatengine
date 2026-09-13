using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardActionEconomy : IActionEconomy, IStateful<ActionEconomyState>
    {
        public bool HasAction { get; private set; } = true;
        public bool HasBonusAction { get; private set; } = true;
        public bool HasReaction { get; private set; } = true;

        // A count, not a bool: GrantFreeObjectInteraction() adds to it rather
        // than just re-setting a flag, so a feature granting a second free
        // interaction this turn actually stacks instead of being a no-op
        // against an already-true flag. ActionEconomyState only carries a
        // bool, though (matching every other resource here) — a count above
        // 1 collapses to "available" on GetState()/restore, so a granted
        // surplus doesn't survive a save/load happening mid-turn. Acceptable
        // for now: nothing grants more than one today (this is an
        // extensibility hook for a future feat, not yet a real consumer),
        // and every other action-economy resource in this engine is already
        // boolean.
        private int _freeObjectInteractionsRemaining = 1;
        public bool HasFreeObjectInteraction => _freeObjectInteractionsRemaining > 0;

        public StandardActionEconomy()
        {
        }

        public StandardActionEconomy(ActionEconomyState state)
        {
            System.ArgumentNullException.ThrowIfNull(state);
            HasAction = state.HasAction;
            HasBonusAction = state.HasBonusAction;
            HasReaction = state.HasReaction;
            _freeObjectInteractionsRemaining = state.HasFreeObjectInteraction ? 1 : 0;
        }

        public ActionEconomyState GetState() => new(HasAction, HasBonusAction, HasReaction, HasFreeObjectInteraction);

        public void UseAction()
        {
            HasAction = false;
        }

        public void UseBonusAction()
        {
            HasBonusAction = false;
        }

        public void UseReaction()
        {
            HasReaction = false;
        }

        public bool TryUseFreeObjectInteraction()
        {
            if (_freeObjectInteractionsRemaining <= 0) return false;
            _freeObjectInteractionsRemaining--;
            return true;
        }

        public void GrantFreeObjectInteraction()
        {
            _freeObjectInteractionsRemaining++;
        }

        public void ResetTurn()
        {
            HasAction = true;
            HasBonusAction = true;
            _freeObjectInteractionsRemaining = 1;
            // Reactions also reset at start of turn in 5e
            ResetReaction();
        }

        public void ResetReaction()
        {
            HasReaction = true;
        }
    }
}
