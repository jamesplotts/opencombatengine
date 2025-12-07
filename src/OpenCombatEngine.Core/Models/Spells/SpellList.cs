using System;
using System.Collections.Generic;

namespace OpenCombatEngine.Core.Models.Spells
{
    public class SpellList
    {
        public string Name { get; }
        public IReadOnlySet<string> Spells { get; }

        public SpellList(string name, IEnumerable<string> spells)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            Name = name;
            Spells = new HashSet<string>(spells ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public bool Contains(string spellName)
        {
            return Spells.Contains(spellName);
        }
    }
}
