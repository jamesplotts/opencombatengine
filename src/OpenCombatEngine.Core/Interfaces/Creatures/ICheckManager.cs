using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
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
        /// <returns>
        /// The full roll detail (individual die, modifier, and the final
        /// total after ability/proficiency modifiers and any active
        /// effects) — not just the total, so a consumer (e.g. a client
        /// rendering a dice-tray animation, or detecting a natural 20/1)
        /// has the actual die result to show, not only the sum.
        /// </returns>
        Result<DiceRollResult> RollAbilityCheck(Ability ability, string? skillName = null);

        /// <summary>
        /// Rolls a saving throw for the specified ability.
        /// Includes the ability modifier and proficiency bonus if applicable.
        /// </summary>
        /// <param name="ability">The ability to save against.</param>
        /// <returns>
        /// The full roll detail — see <see cref="RollAbilityCheck"/>'s
        /// return docs for why this isn't just the total.
        /// </returns>
        Result<DiceRollResult> RollSavingThrow(Ability ability);

        /// <summary>
        /// Rolls a death saving throw.
        /// </summary>
        /// <returns>
        /// The full roll detail — see <see cref="RollAbilityCheck"/>'s
        /// return docs for why this isn't just the total.
        /// </returns>
        Result<DiceRollResult> RollDeathSave();

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

        /// <summary>
        /// Fired when a saving throw is rolled.
        /// </summary>
        event EventHandler<OpenCombatEngine.Core.Models.Events.SavingThrowEventArgs> SavingThrowRolled;
    }
}
