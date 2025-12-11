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
        private readonly List<ICreature> _participants = new();

        public StandardCombatManager(ITurnManager turnManager, IGridManager? gridManager = null)
        {
            _turnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
            _gridManager = gridManager;
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
            // Group active creatures (HP > 0 or not dead state) by Team
            // StandardCreature defines "IsDead".
            
            var activeTeams = _participants
                .Where(p => p.HitPoints.Current > 0 && !p.HitPoints.IsDead)
                .Select(p => p.Team)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (activeTeams.Count <= 1)
            {
                // Win Condition Met
                string winner = activeTeams.FirstOrDefault() ?? "Draw";
                EndEncounter(winner);
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
