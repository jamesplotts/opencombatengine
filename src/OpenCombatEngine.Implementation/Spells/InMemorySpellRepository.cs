using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Spells
{
    /// <summary>
    /// A simple in-memory repository for spells.
    /// </summary>
    public class InMemorySpellRepository : ISpellRepository
    {
        private readonly Dictionary<string, ISpell> _spells = new(StringComparer.OrdinalIgnoreCase);

        public void AddSpell(ISpell spell)
        {
            ArgumentNullException.ThrowIfNull(spell);
            _spells[spell.Name] = spell;
        }

        public Result<ISpell> GetSpell(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<ISpell>.Failure("Spell name cannot be empty.");

            if (_spells.TryGetValue(name, out var spell))
            {
                return Result<ISpell>.Success(spell);
            }

            return Result<ISpell>.Failure($"Spell '{name}' not found.");
        }
    }
}
