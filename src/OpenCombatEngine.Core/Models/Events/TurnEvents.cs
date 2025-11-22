using System;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Core.Models.Events
{
    public class TurnChangedEventArgs : EventArgs
    {
        public ICreature Creature { get; }
        public int Round { get; }

        public TurnChangedEventArgs(ICreature creature, int round)
        {
            Creature = creature;
            Round = round;
        }
    }

    public class RoundChangedEventArgs : EventArgs
    {
        public int NewRound { get; }

        public RoundChangedEventArgs(int newRound)
        {
            NewRound = newRound;
        }
    }

    public class CombatEndedEventArgs : EventArgs
    {
        public CombatEndedEventArgs() { }
    }
}
