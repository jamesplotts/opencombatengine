using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Combat;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Implementation.Creatures; // For StandardCreature reconstruction

namespace OpenCombatEngine.Implementation.Combat
{
    public class StandardCombatManager : ICombatManager, IStateful<CombatState>
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
        public CombatState GetState()
        {
            // Serialize participants
            var participantStates = _participants
                .OfType<IStateful<CreatureState>>()
                .Select(p => p.GetState())
                .ToList();

            // Get TurnManager state
            var turnState = (_turnManager as IStateful<TurnManagerState>)?.GetState() 
                ?? throw new InvalidOperationException("TurnManager does not support state export.");

            // Store WinCondition type (optional)
            var winType = _winCondition.GetType().FullName;

            return new CombatState(participantStates, turnState, winType);
        }

        public void RestoreState(CombatState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            // Restore participants
            _participants.Clear();
            foreach (var pState in state.Participants)
            {
                var creature = new StandardCreature(pState);
                creature.HitPoints.Died += OnParticipantDied;
                _participants.Add(creature);
            }

            // Restore TurnManager
            // Note: TurnManager needs the restored creatures to rebuild its turn order references
            if (_turnManager is StandardTurnManager stdTm) 
            {
                stdTm.RestoreState(state.TurnManager, _participants);
            }
            else if (_turnManager is IStateful<TurnManagerState> statefulTm)
            {
                // If it's another implementation that supports state but lacks the creature injection overload?
                // The IStateful<T> interface usually only has RestoreState(T). 
                // Wait, IStateful<T> defines void RestoreState(T state).
                // StandardTurnManager.RestoreState needs creatures.
                // It should probably implement a specific method or we just cast and call it.
                // StandardTurnManager implements `IStateful<TurnManagerState>` but needs extra data.
                // Actually, `IStateful<T>` doesn't support extra args.
                // So StandardTurnManager.RestoreState(state) would fail or do nothing if it needs creatures.
                // I added `RestoreState(TurnManagerState, IEnumerable<ICreature>)` which is NOT the interface method.
                // So I need to cast to StandardTurnManager specifically.
                throw new NotSupportedException("TurnManager does not support restoring state without creature references in this context.");
            }
            
            // Restore WinCondition? 
            // For now, we keep existing win condition logic unless we want to reflectively instantiate from WinConditionType string.
            // As per plan, keeping it simple. Maybe log a warning if types mismatch?
        }
    }
}
