using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;

namespace OpenCombatEngine.Implementation.Combat
{
    public class StandardCombatManager : ICombatManager
    {
        public event EventHandler<EventArgs>? EncounterStarted;
        public event EventHandler<EncounterEndedEventArgs>? EncounterEnded;

        private readonly ITurnManager _turnManager;
        private readonly IGridManager? _gridManager;
        private readonly IWinCondition _winCondition;
        private readonly List<ICreature> _participants = new();

        public IReadOnlyList<ICreature> Participants => _participants.AsReadOnly();

        public StandardCombatManager(
            ITurnManager turnManager, 
            IGridManager? gridManager = null,
            IWinCondition? winCondition = null)
        {
            _turnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
            _gridManager = gridManager;
            _winCondition = winCondition ?? new OpenCombatEngine.Implementation.Combat.WinConditions.LastTeamStandingWinCondition();
        }

        public void StartEncounter(IEnumerable<ICreature> participants)
        {
            ArgumentNullException.ThrowIfNull(participants);
            
            _participants.Clear();
            _participants.AddRange(participants);

            if (_participants.Count == 0) throw new ArgumentException("Cannot start encounter with no participants.");

            // Subscribe to death events
            foreach (var p in _participants)
            {
                p.HitPoints.Died += OnParticipantDied;
            }

            // Start Turns
            _turnManager.StartCombat(_participants);

            EncounterStarted?.Invoke(this, EventArgs.Empty);
        }

        private void OnParticipantDied(object? sender, EventArgs e)
        {
            CheckWinCondition();
        }

        public void CheckWinCondition()
        {
            if (_winCondition.Check(this))
            {
                EndEncounter(_winCondition.GetWinner(this));
            }
        }

        public void EndEncounter()
        {
            EndEncounter("Terminated");
        }

        private void EndEncounter(string winner)
        {
            _turnManager.EndCombat();
            
            foreach (var p in _participants)
            {
                p.HitPoints.Died -= OnParticipantDied;
            }

            EncounterEnded?.Invoke(this, new EncounterEndedEventArgs(winner));
        }
    }
}
