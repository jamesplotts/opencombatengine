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

        public StandardActionEconomy()
        {
        }

        public StandardActionEconomy(ActionEconomyState state)
        {
            System.ArgumentNullException.ThrowIfNull(state);
            HasAction = state.HasAction;
            HasBonusAction = state.HasBonusAction;
            HasReaction = state.HasReaction;
        }

        public ActionEconomyState GetState() => new(HasAction, HasBonusAction, HasReaction);

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

        public void ResetTurn()
        {
            HasAction = true;
            HasBonusAction = true;
            // Reactions also reset at start of turn in 5e
            ResetReaction();
        }

        public void ResetReaction()
        {
            HasReaction = true;
        }
    }
}
