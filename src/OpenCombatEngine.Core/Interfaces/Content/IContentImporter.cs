using System.Collections.Generic;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Content
{
    /// <summary>
    /// Interface for importing content from a data source.
    /// </summary>
    /// <typeparam name="T">The type of content to import (e.g. ISpell).</typeparam>
    public interface IContentImporter<T>
    {
        /// <summary>
        /// Imports content from the provided string data (e.g. JSON).
        /// </summary>
        /// <param name="data">The raw data string.</param>
        /// <returns>A result containing the collection of imported items.</returns>
        Result<IEnumerable<T>> Import(string data);
    }
}
