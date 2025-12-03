using System;

namespace OpenCombatEngine.Core.Models.Events
{
    public class DamageTakenEventArgs : EventArgs
    {
        public int Amount { get; }
        public OpenCombatEngine.Core.Enums.DamageType Type { get; }
        public int CurrentHealth { get; }
        public int TemporaryHealth { get; }

        public DamageTakenEventArgs(int amount, OpenCombatEngine.Core.Enums.DamageType type, int currentHealth, int temporaryHealth)
        {
            Amount = amount;
            Type = type;
            CurrentHealth = currentHealth;
            TemporaryHealth = temporaryHealth;
        }
    }

    public class HealedEventArgs : EventArgs
    {
        public int Amount { get; }
        public int CurrentHealth { get; }

        public HealedEventArgs(int amount, int currentHealth)
        {
            Amount = amount;
            CurrentHealth = currentHealth;
        }
    }

    public class DeathEventArgs : EventArgs
    {
        public DeathEventArgs() { }
    }
}
