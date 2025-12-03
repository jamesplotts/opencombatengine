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

        public ClassDefinition(string name, int hitDie, Dictionary<int, IEnumerable<IFeature>>? featuresByLevel = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (hitDie <= 0) throw new ArgumentException("Hit die must be positive", nameof(hitDie));

            Name = name;
            HitDie = hitDie;
            FeaturesByLevel = featuresByLevel ?? new Dictionary<int, IEnumerable<IFeature>>();
        }
    }
}
