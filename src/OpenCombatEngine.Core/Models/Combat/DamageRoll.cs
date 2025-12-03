using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Models.Combat
{
    public class DamageRoll
    {
        public int Amount { get; }
        public DamageType Type { get; }

        public DamageRoll(int amount, DamageType type)
        {
            Amount = amount;
            Type = type;
        }
    }
}
