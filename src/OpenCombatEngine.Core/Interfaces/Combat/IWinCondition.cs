using OpenCombatEngine.Core.Interfaces.Combat;

namespace OpenCombatEngine.Core.Interfaces.Combat
{
    /// <summary>
    /// Defines logic for determining if a combat encounter has been won or should end.
    /// </summary>
    public interface IWinCondition
    {
        /// <summary>
        /// Checks if the win condition has been met.
        /// </summary>
        /// <param name="combatManager">The combat manager context.</param>
        /// <returns>True if the condition is met (encounters ends), false otherwise.</returns>
        bool Check(ICombatManager combatManager);

        /// <summary>
        /// Gets the name of the winning team or reason for ending.
        /// Should be called only if Check returns true.
        /// </summary>
        /// <param name="combatManager">The combat manager context.</param>
        /// <returns>Name of the winning team (e.g. "Players", "Monsters") or "Draw".</returns>
        string GetWinner(ICombatManager combatManager);
    }
}
