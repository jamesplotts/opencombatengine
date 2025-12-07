using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Features;

namespace OpenCombatEngine.Core.Interfaces.Classes
{
    /// <summary>
    /// Defines a character class (e.g. Fighter, Wizard).
    /// </summary>
    public interface IClassDefinition
    {
        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the hit die size (e.g. 8 for d8).
        /// </summary>
        int HitDie { get; }

        /// <summary>
        /// Gets the features granted by this class at each level.
        /// </summary>
        IReadOnlyDictionary<int, IEnumerable<IFeature>> FeaturesByLevel { get; }

        /// <summary>
        /// Gets the spell list for this class, if any.
        /// </summary>
        OpenCombatEngine.Core.Models.Spells.SpellList? SpellList { get; }
    }
}
