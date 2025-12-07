using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Core.Models.Events
{
    public class SavingThrowEventArgs : EventArgs
    {
        public Ability Ability { get; }
        public int Result { get; }
        public ICreature Roller { get; }

        public SavingThrowEventArgs(Ability ability, int result, ICreature roller)
        {
            Ability = ability;
            Result = result;
            Roller = roller;
        }
    }
}
