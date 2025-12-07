using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Classes;
using OpenCombatEngine.Core.Interfaces.Features;

namespace OpenCombatEngine.Implementation.Classes
{
    public class ClassDefinition : IClassDefinition
    {
        public string Name { get; }
        public int HitDie { get; }
        public IReadOnlyDictionary<int, IEnumerable<IFeature>> FeaturesByLevel { get; }
        public OpenCombatEngine.Core.Models.Spells.SpellList? SpellList { get; }
        public OpenCombatEngine.Core.Enums.SpellcastingType SpellcastingType { get; }

        public ClassDefinition(
            string name, 
            int hitDie, 
            Dictionary<int, IEnumerable<IFeature>>? featuresByLevel = null, 
            OpenCombatEngine.Core.Models.Spells.SpellList? spellList = null,
            OpenCombatEngine.Core.Enums.SpellcastingType spellcastingType = OpenCombatEngine.Core.Enums.SpellcastingType.None)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (hitDie <= 0) throw new ArgumentException("Hit die must be positive", nameof(hitDie));

            Name = name;
            HitDie = hitDie;
            FeaturesByLevel = featuresByLevel ?? new Dictionary<int, IEnumerable<IFeature>>();
            SpellList = spellList;
            SpellcastingType = spellcastingType;
        }
    }
}
