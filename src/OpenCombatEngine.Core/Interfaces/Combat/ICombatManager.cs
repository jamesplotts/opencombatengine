using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Events;

namespace OpenCombatEngine.Core.Interfaces.Combat
{
    public interface ICombatManager
    {
        event EventHandler<EventArgs> EncounterStarted;
        event EventHandler<EncounterEndedEventArgs> EncounterEnded;

        IReadOnlyList<ICreature> Participants { get; }

        void StartEncounter(IEnumerable<ICreature> participants);
        void EndEncounter();
        void CheckWinCondition();
    }

    public class EncounterEndedEventArgs : EventArgs
    {
        public string WinningTeam { get; }
        public EncounterEndedEventArgs(string winningTeam)
        {
            WinningTeam = winningTeam;
        }
    }
}
