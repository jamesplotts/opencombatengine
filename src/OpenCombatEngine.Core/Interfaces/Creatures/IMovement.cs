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

        /// <summary>
        /// Gets or sets whether the creature is currently in difficult terrain.
        /// </summary>
        bool IsInDifficultTerrain { get; set; }

        /// <summary>
        /// Fired when the creature moves.
        /// </summary>
        event System.EventHandler<OpenCombatEngine.Core.Models.Events.MovedEventArgs> Moved;

        /// <summary>
        /// Notifies the movement component that a move has occurred (physically).
        /// Used by GridManager to trigger the Moved event.
        /// </summary>
        /// <param name="from">Starting position.</param>
        /// <param name="destination">Ending position.</param>
        void NotifyMoved(OpenCombatEngine.Core.Models.Spatial.Position from, OpenCombatEngine.Core.Models.Spatial.Position destination);
    }
}
