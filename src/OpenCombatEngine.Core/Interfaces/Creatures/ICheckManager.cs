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
        /// Includes the ability modifier and proficiency bonus if applicable.
        /// </summary>
        /// <param name="ability">The ability to check.</param>
        /// <param name="skillName">Optional skill name to check for proficiency.</param>
        /// <returns>The result of the check.</returns>
        Result<int> RollAbilityCheck(Ability ability, string? skillName = null);

        /// <summary>
        /// Rolls a saving throw for the specified ability.
        /// Includes the ability modifier and proficiency bonus if applicable.
        /// </summary>
        /// <param name="ability">The ability to save against.</param>
        /// <returns>The result of the saving throw.</returns>
        Result<int> RollSavingThrow(Ability ability);

        /// <summary>
        /// Rolls a death saving throw.
        /// </summary>
        /// <returns>The result of the death save.</returns>
        Result<int> RollDeathSave();

        /// <summary>
        /// Adds proficiency in a skill.
        /// </summary>
        void AddSkillProficiency(string skillName);

        /// <summary>
        /// Removes proficiency in a skill.
        /// </summary>
        void RemoveSkillProficiency(string skillName);

        /// <summary>
        /// Checks if the creature is proficient in a skill.
        /// </summary>
        bool HasSkillProficiency(string skillName);

        /// <summary>
        /// Adds proficiency in a saving throw.
        /// </summary>
        void AddSavingThrowProficiency(Ability ability);

        /// <summary>
        /// Removes proficiency in a saving throw.
        /// </summary>
        void RemoveSavingThrowProficiency(Ability ability);

        /// <summary>
        /// Checks if the creature is proficient in a saving throw.
        /// </summary>
        bool HasSavingThrowProficiency(Ability ability);
    }
}
