namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a class level.
    /// </summary>
    /// <param name="ClassName">Name of the class.</param>
    /// <param name="Level">Level in this class.</param>
    /// <param name="HitDie">Hit die of the class.</param>
    public record ClassLevelState(string ClassName, int Level, int HitDie);
}
