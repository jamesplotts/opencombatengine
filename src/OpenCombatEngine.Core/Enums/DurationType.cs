namespace OpenCombatEngine.Core.Enums
{
    /// <summary>
    /// Defines the duration type of an effect.
    /// </summary>
    public enum DurationType
    {
        Unspecified = 0,
        
        /// <summary>
        /// The effect applies instantly and has no lingering duration (e.g., instant damage/healing).
        /// </summary>
        Instant,
        
        /// <summary>
        /// The effect lasts for a specific number of rounds.
        /// </summary>
        Round,
        
        /// <summary>
        /// The effect lasts for a specific number of minutes.
        /// (1 Minute = 10 Rounds).
        /// </summary>
        Minute,
        
        /// <summary>
        /// The effect lasts for a specific number of hours.
        /// (1 Hour = 60 Minutes = 600 Rounds).
        /// </summary>
        Hour,
        
        /// <summary>
        /// The effect lasts for a specific number of days.
        /// </summary>
        Day,
        
        /// <summary>
        /// The effect lasts until the end of the current turn.
        /// </summary>
        UntilEndOfTurn,
        
        /// <summary>
        /// The effect lasts until the start of the target's next turn.
        /// </summary>
        UntilStartOfNextTurn,
        
        /// <summary>
        /// The effect is permanent until removed.
        /// </summary>
        Permanent,
        
        LastValue
    }
}
