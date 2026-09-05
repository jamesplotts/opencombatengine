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

        /// <summary>
        /// Returns every spell currently in the repository — used by
        /// character-creation's spell-pick prompts and the ListClassSpells
        /// RPC to filter down to one class's real spell list (via
        /// <see cref="ISpell.Classes"/>) and level, rather than a second,
        /// hand-authored list. A default interface member, not required:
        /// the repository already holds every spell in-memory (see
        /// <c>InMemorySpellRepository</c>, the one production
        /// implementation, which overrides this with a real enumeration),
        /// so any other implementer that predates this capability keeps
        /// compiling with an empty result rather than being forced to add
        /// it.
        /// </summary>
        System.Collections.Generic.IEnumerable<ISpell> GetAllSpells() => System.Array.Empty<ISpell>();
    }
}
