namespace OpenCombatEngine.Core.Enums
{
    /// <summary>
    /// Defines the types of damage in the game.
    /// </summary>
    public enum DamageType
    {
        Unspecified = 0,
        
        // Physical
        Bludgeoning,
        Piercing,
        Slashing,
        
        // Elemental/Magic
        Acid,
        Cold,
        Fire,
        Force,
        Lightning,
        Necrotic,
        Poison,
        Psychic,
        Radiant,
        Thunder,
        
        LastValue
    }
}
