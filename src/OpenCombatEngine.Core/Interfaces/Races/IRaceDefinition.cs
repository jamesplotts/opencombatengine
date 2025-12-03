using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Features;

namespace OpenCombatEngine.Core.Interfaces.Races
{
    /// <summary>
    /// Defines a character race (e.g. Human, Elf).
    /// </summary>
    public interface IRaceDefinition
    {
        /// <summary>
        /// Gets the name of the race.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the base walking speed of the race in feet.
        /// </summary>
        int Speed { get; }

        /// <summary>
        /// Gets the size category of the race.
        /// </summary>
        Size Size { get; }

        /// <summary>
        /// Gets the ability score increases granted by this race.
        /// </summary>
        IReadOnlyDictionary<Ability, int> AbilityScoreIncreases { get; }

        /// <summary>
        /// Gets the features granted by this race.
        /// </summary>
        IEnumerable<IFeature> RacialFeatures { get; }
    }
}
