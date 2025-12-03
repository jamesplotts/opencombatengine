using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Spells
{
    /// <summary>
    /// Provides access to spell definitions.
    /// </summary>
    public interface ISpellRepository
    {
        /// <summary>
        /// Retrieves a spell by its name.
        /// </summary>
        /// <param name="name">The name of the spell to retrieve.</param>
        /// <returns>The spell if found, otherwise a failure result.</returns>
        Result<ISpell> GetSpell(string name);

        /// <summary>
        /// Adds a spell to the repository.
        /// </summary>
        /// <param name="spell">The spell to add.</param>
        void AddSpell(ISpell spell);
    }
}
