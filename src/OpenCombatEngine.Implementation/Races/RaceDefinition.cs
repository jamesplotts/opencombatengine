using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Races;

namespace OpenCombatEngine.Implementation.Races
{
    public class RaceDefinition : IRaceDefinition
    {
        public string Name { get; }
        public int Speed { get; }
        public Size Size { get; }
        public IReadOnlyDictionary<Ability, int> AbilityScoreIncreases { get; }
        public IEnumerable<IFeature> RacialFeatures { get; }

        public RaceDefinition(
            string name, 
            int speed, 
            Size size, 
            Dictionary<Ability, int>? abilityScoreIncreases = null, 
            IEnumerable<IFeature>? racialFeatures = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (speed < 0) throw new ArgumentException("Speed cannot be negative", nameof(speed));

            Name = name;
            Speed = speed;
            Size = size;
            AbilityScoreIncreases = abilityScoreIncreases ?? new Dictionary<Ability, int>();
            RacialFeatures = racialFeatures ?? Enumerable.Empty<IFeature>();
        }
    }
}
