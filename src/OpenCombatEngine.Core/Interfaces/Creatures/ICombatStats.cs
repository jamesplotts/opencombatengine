namespace OpenCombatEngine.Core.Interfaces.Creatures
{
    /// <summary>
    /// Encapsulates combat-related statistics for a creature.
    /// </summary>
    public interface ICombatStats
    {
        /// <summary>
        /// Gets the Armor Class (AC) of the creature.
        /// Represents how difficult it is to hit the creature.
        /// </summary>
        int ArmorClass { get; }

        /// <summary>
        /// Gets the initiative bonus.
        /// Added to d20 rolls to determine turn order.
        /// </summary>
        int InitiativeBonus { get; }

        /// <summary>
        /// Gets the movement speed in feet.
        /// </summary>
        int Speed { get; }
    }
}
