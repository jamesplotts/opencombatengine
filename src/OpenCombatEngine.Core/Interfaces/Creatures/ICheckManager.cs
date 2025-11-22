using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Creatures
{
    /// <summary>
    /// Manages ability checks and saving throws for a creature.
    /// </summary>
    public interface ICheckManager
    {
        /// <summary>
        /// Rolls an ability check for the specified ability.
        /// Includes the ability modifier.
        /// </summary>
        /// <param name="ability">The ability to check.</param>
        /// <returns>The result of the check.</returns>
        Result<int> RollAbilityCheck(Ability ability);

        /// <summary>
        /// Rolls a saving throw for the specified ability.
        /// Includes the ability modifier and proficiency bonus if applicable.
        /// </summary>
        /// <param name="ability">The ability to save against.</param>
        /// <returns>The result of the saving throw.</returns>
        Result<int> RollSavingThrow(Ability ability);
    }
}
