using System.Collections.Generic;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a combat encounter.
    /// </summary>
    /// <param name="Participants">List of serialized participants.</param>
    /// <param name="TurnManager">State of the turn manager.</param>
    /// <param name="WinConditionType">Optional type name of the win condition logic.</param>
#pragma warning disable CA1002 // Do not expose generic lists
    public record CombatState(
        List<CreatureState> Participants,
        TurnManagerState TurnManager,
        string? WinConditionType = null);
#pragma warning restore CA1002
}
