using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Core.Models
{
    /// <summary>
    /// Represents a creature's initiative roll result.
    /// Used for sorting turn order.
    /// </summary>
    /// <param name="Creature">The creature who rolled.</param>
    /// <param name="Total">The total initiative roll (d20 + bonus).</param>
    /// <param name="DexterityScore">The creature's Dexterity score (used for tie-breaking).</param>
    public record InitiativeRoll(ICreature Creature, int Total, int DexterityScore);
}
