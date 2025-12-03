using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Results;
using System.Collections.Generic;

namespace OpenCombatEngine.Core.Interfaces.Conditions
{
    /// <summary>
    /// Manages the conditions applied to a creature.
    /// </summary>
    public interface IConditionManager
    {
        /// <summary>
        /// Gets the currently active conditions.
        /// </summary>
        IEnumerable<ICondition> ActiveConditions { get; }

        /// <summary>
        /// Checks if the creature has a specific condition type.
        /// </summary>
        /// <param name="type">The condition type to check.</param>
        /// <returns>True if the creature has the condition, otherwise false.</returns>
        bool HasCondition(ConditionType type);

        /// <summary>
        /// Adds a condition to the manager.
        /// </summary>
        /// <param name="condition">The condition to add.</param>
        /// <returns>Success if added, Failure if invalid.</returns>
        Result<bool> AddCondition(ICondition condition);

        /// <summary>
        /// Removes a condition by name.
        /// </summary>
        /// <param name="conditionName">The name of the condition to remove.</param>
        void RemoveCondition(string conditionName);

        /// <summary>
        /// Processes turn-start logic for all conditions (e.g., decrementing duration).
        /// </summary>
        void Tick();
    }
}
