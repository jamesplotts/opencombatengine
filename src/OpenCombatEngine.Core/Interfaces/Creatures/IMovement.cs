namespace OpenCombatEngine.Core.Interfaces.Creatures
{
    /// <summary>
    /// Tracks the movement capabilities and remaining movement for a creature.
    /// </summary>
    public interface IMovement
    {
        /// <summary>
        /// Gets the base speed of the creature in feet (derived from stats).
        /// </summary>
        int Speed { get; }

        /// <summary>
        /// Gets the remaining movement available for the current turn in feet.
        /// </summary>
        int MovementRemaining { get; }

        /// <summary>
        /// Moves the creature by the specified distance.
        /// </summary>
        /// <param name="distance">The distance to move in feet.</param>
        void Move(int distance);

        /// <summary>
        /// Resets the remaining movement to the creature's current speed.
        /// Typically called at the start of the turn.
        /// </summary>
        void ResetTurn();
    }
}
