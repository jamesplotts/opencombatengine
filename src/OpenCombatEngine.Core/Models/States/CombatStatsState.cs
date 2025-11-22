namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for combat statistics.
    /// </summary>
    /// <param name="ArmorClass">Armor Class.</param>
    /// <param name="InitiativeBonus">Initiative Bonus.</param>
    /// <param name="Speed">Movement Speed.</param>
    public record CombatStatsState(int ArmorClass, int InitiativeBonus, int Speed);
}
