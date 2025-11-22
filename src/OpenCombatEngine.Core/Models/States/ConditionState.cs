using OpenCombatEngine.Core.Enums;

namespace OpenCombatEngine.Core.Models.States
{
    /// <summary>
    /// Serializable state for a condition.
    /// </summary>
    /// <param name="Name">Condition name.</param>
    /// <param name="Description">Condition description.</param>
    /// <param name="DurationRounds">Remaining duration in rounds.</param>
    /// <param name="Type">Condition type.</param>
    public record ConditionState(string Name, string Description, int DurationRounds, ConditionType Type);
}
