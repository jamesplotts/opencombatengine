using System;
using System.Collections.Generic;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for the turn manager.
    /// </summary>
    /// <param name="CurrentRound">The current round number.</param>
    /// <param name="CurrentTurnIndex">The index of the current creature's turn.</param>
    /// <param name="TurnOrderIds">Ordered list of creature IDs representing the initiative order.</param>
#pragma warning disable CA1002 // Do not expose generic lists
    public record TurnManagerState(
        int CurrentRound,
        int CurrentTurnIndex,
        List<Guid> TurnOrderIds);
#pragma warning restore CA1002
}
