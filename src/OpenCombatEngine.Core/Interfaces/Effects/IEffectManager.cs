using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Effects
{
    /// <summary>
    /// Manages the active effects applied to a creature.
    /// </summary>
    public interface IEffectManager
    {
        /// <summary>
        /// Gets the currently active effects.
        /// </summary>
        IEnumerable<IActiveEffect> ActiveEffects { get; }

        /// <summary>
        /// Adds an effect to the manager.
        /// </summary>
        Result<bool> AddEffect(IActiveEffect effect);

        /// <summary>
        /// Removes an effect by name.
        /// </summary>
        void RemoveEffect(string effectName);

        /// <summary>
        /// Processes turn-start logic for all effects.
        /// </summary>
        void Tick();

        /// <summary>
        /// Applies all active effects to a base stat value.
        /// </summary>
        /// <param name="stat">The stat to modify.</param>
        /// <param name="baseValue">The base value of the stat.</param>
        /// <returns>The final value after all modifications.</returns>
        int ApplyStatBonuses(StatType stat, int baseValue);
    }
}
