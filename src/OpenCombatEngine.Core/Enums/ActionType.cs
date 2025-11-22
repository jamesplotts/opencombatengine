namespace OpenCombatEngine.Core.Enums
{
    /// <summary>
    /// Defines the types of actions a creature can take.
    /// </summary>
    public enum ActionType
    {
        Unspecified = 0,
        
        /// <summary>
        /// A standard action (Attack, Cast Spell, Dash, etc.).
        /// </summary>
        Action,
        
        /// <summary>
        /// A faster action (Off-hand attack, some spells).
        /// </summary>
        BonusAction,
        
        /// <summary>
        /// An action taken in response to a trigger (Opportunity Attack, Shield spell).
        /// </summary>
        Reaction,
        
        /// <summary>
        /// Special actions available to legendary creatures.
        /// </summary>
        LegendaryAction,

        /// <summary>
        /// Movement.
        /// </summary>
        Movement,
        
        LastValue
    }
}
