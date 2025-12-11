using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models;
using OpenCombatEngine.Core.Models.Events;

using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Implementation
{
    public class StandardTurnManager : ITurnManager, IStateful<TurnManagerState>
    {
        public event EventHandler<TurnChangedEventArgs>? TurnChanged;
        public event EventHandler<RoundChangedEventArgs>? RoundChanged;
        public event EventHandler<CombatEndedEventArgs>? CombatEnded;

        private readonly IDiceRoller _diceRoller;
        private readonly IInitiativeComparer _initiativeComparer;
        private readonly List<ICreature> _turnOrder = new();
        private int _currentTurnIndex = -1;

        public int CurrentRound { get; private set; }

        public ICreature? CurrentCreature => 
            _currentTurnIndex >= 0 && _currentTurnIndex < _turnOrder.Count 
                ? _turnOrder[_currentTurnIndex] 
                : null;

        public IEnumerable<ICreature> TurnOrder => _turnOrder.AsReadOnly();

        public StandardTurnManager(IDiceRoller diceRoller, IInitiativeComparer? initiativeComparer = null)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            _initiativeComparer = initiativeComparer ?? new OpenCombatEngine.Implementation.Comparers.StandardInitiativeComparer();
        }

        public void StartCombat(IEnumerable<ICreature> creatures)
        {
            ArgumentNullException.ThrowIfNull(creatures);
            
            var creatureList = creatures.ToList();
            if (creatureList.Count == 0) throw new ArgumentException("Cannot start combat with no creatures", nameof(creatures));

            _turnOrder.Clear();
            var initiativeRolls = new List<InitiativeRoll>();

            foreach (var creature in creatureList)
            {
                // Roll Initiative: d20 + InitiativeBonus
                // Note: InitiativeBonus is in CombatStats. If null, assume 0.
                // Actually, StandardCreature ensures CombatStats is not null, but interface allows it? 
                // Interface definition: ICombatStats CombatStats { get; } - it's not nullable in the interface.
                
                int bonus = creature.CombatStats.InitiativeBonus;
                var rollResult = _diceRoller.Roll($"1d20+{bonus}");
                
                // If roll fails (shouldn't happen with standard roller), default to bonus (effectively rolling 0) or throw?
                // Let's assume success for now, or default to 0 + bonus.
                int total = rollResult.IsSuccess ? rollResult.Value.Total : bonus;

                initiativeRolls.Add(new InitiativeRoll(creature, total, creature.AbilityScores.Dexterity));
            }

            // Sort using the comparer
            // StandardInitiativeComparer returns > 0 if x > y (High Roll > Low Roll).
            // List.Sort sorts Ascending (Smallest first).
            // So we sort, then Reverse to get Descending (Highest first).
            initiativeRolls.Sort(_initiativeComparer);
            initiativeRolls.Reverse();

            var sortedCreatures = initiativeRolls.Select(x => x.Creature).ToList();

            _turnOrder.AddRange(sortedCreatures);
            
            CurrentRound = 1;
            _currentTurnIndex = -1;
            NextTurn();
        }

        public void NextTurn()
        {
            if (_turnOrder.Count == 0) return;

            // Maximum loops to prevent infinite loop if everyone is dead
            int checks = 0;
            int maxChecks = _turnOrder.Count * 2; // Allow for a couple of rounds of checks just in case

            do
            {
                checks++;
                _currentTurnIndex++;

                if (_currentTurnIndex >= _turnOrder.Count)
                {
                    _currentTurnIndex = 0;
                    CurrentRound++;
                    RoundChanged?.Invoke(this, new RoundChangedEventArgs(CurrentRound));
                }

                // If check count exceeds limit, stop. Maybe everyone is dead.
                if (checks > maxChecks) return;

            } while (_turnOrder[_currentTurnIndex].HitPoints.IsDead);

            var currentCreature = CurrentCreature;
            if (currentCreature != null)
            {
                currentCreature.StartTurn();
                TurnChanged?.Invoke(this, new TurnChangedEventArgs(currentCreature, CurrentRound));
            }
        }

        public void EndCombat()
        {
            _turnOrder.Clear();
            CurrentRound = 0;
            _currentTurnIndex = -1;
            CombatEnded?.Invoke(this, new CombatEndedEventArgs());
        }
        public TurnManagerState GetState()
        {
            var orderIds = _turnOrder.Select(c => c.Id).ToList();
            return new TurnManagerState(CurrentRound, _currentTurnIndex, orderIds);
        }

        public void RestoreState(TurnManagerState state, IEnumerable<ICreature> availableCreatures)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(availableCreatures);

            CurrentRound = state.CurrentRound;
            _currentTurnIndex = state.CurrentTurnIndex;
            
            _turnOrder.Clear();
            var creatureMap = availableCreatures.ToDictionary(c => c.Id);

            foreach (var id in state.TurnOrderIds)
            {
                if (creatureMap.TryGetValue(id, out var creature))
                {
                    _turnOrder.Add(creature);
                }
                // If creature not found, skip? Or fail? Skipping is safer for partial loads.
            }
        }
    }
}
