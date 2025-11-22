using System;

namespace OpenCombatEngine.Core.Models.Events
{
    public class DamageTakenEventArgs : EventArgs
    {
        public int Amount { get; }
        public int CurrentHealth { get; }
        public bool IsCritical { get; } // Optional context

        public DamageTakenEventArgs(int amount, int currentHealth, bool isCritical = false)
        {
            Amount = amount;
            CurrentHealth = currentHealth;
            IsCritical = isCritical;
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
