namespace OpenCombatEngine.Core.Enums
{
    /// <summary>
    /// Defines when a magic item regains charges.
    /// </summary>
    public enum RechargeFrequency
    {
        Unspecified = 0,
        
        /// <summary>
        /// Item never recharges automatically.
        /// </summary>
        Never,

        /// <summary>
        /// Recharges daily at dawn.
        /// </summary>
        Dawn,

        /// <summary>
        /// Recharges daily at dusk.
        /// </summary>
        Dusk,

        /// <summary>
        /// Recharges daily at midnight.
        /// </summary>
        Midnight,

        /// <summary>
        /// Recharges after a short rest.
        /// </summary>
        ShortRest,

        /// <summary>
        /// Recharges after a long rest.
        /// </summary>
        LongRest,

        /// <summary>
        /// Recharges on a specific trigger not covered by other values.
        /// </summary>
        Other,

        /// <summary>
        /// Sentinel value for validation.
        /// </summary>
        LastValue
    }
}
