using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardActionEconomy : IActionEconomy
    {
        public bool HasAction { get; private set; } = true;
        public bool HasBonusAction { get; private set; } = true;
        public bool HasReaction { get; private set; } = true;

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
