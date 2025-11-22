namespace OpenCombatEngine.Core.Models.Actions
{
    /// <summary>
    /// Represents the outcome of an executed action.
    /// </summary>
    /// <param name="Success">Whether the action succeeded (e.g., hit the target).</param>
    /// <param name="Message">A descriptive message of the outcome.</param>
    /// <param name="DamageDealt">Amount of damage dealt, if any.</param>
    public record ActionResult(bool Success, string Message, int DamageDealt = 0);
}
