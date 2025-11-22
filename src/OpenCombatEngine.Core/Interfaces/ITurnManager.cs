using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Core.Interfaces
{
    /// <summary>
    /// Manages the flow of combat, including initiative, rounds, and turn order.
    /// </summary>
    public interface ITurnManager
    {
        /// <summary>
        /// Gets the current round number. Starts at 0 before combat begins.
        /// </summary>
        int CurrentRound { get; }

        /// <summary>
        /// Gets the creature whose turn it currently is.
        /// Returns null if combat hasn't started.
        /// </summary>
        ICreature? CurrentCreature { get; }

        /// <summary>
        /// Gets the ordered list of creatures in the initiative order.
        /// </summary>
        IEnumerable<ICreature> TurnOrder { get; }

        /// <summary>
        /// Starts combat with the given participants.
        /// Rolls initiative and sets up the turn order.
        /// </summary>
        /// <param name="creatures">The creatures participating in combat.</param>
        void StartCombat(IEnumerable<ICreature> creatures);

        /// <summary>
        /// Advances to the next turn in the order.
        /// If the end of the order is reached, increments the round and loops back to the start.
        /// </summary>
        void NextTurn();

        /// <summary>
        /// Ends the combat encounter, clearing state.
        /// </summary>
        void EndCombat();
    }
}
