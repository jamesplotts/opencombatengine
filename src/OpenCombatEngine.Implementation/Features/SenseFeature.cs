using System;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    public class SenseFeature : IFeature
    {
        public string Name { get; }
        public string SenseType { get; }
        public int Range { get; }

        public SenseFeature(string name, string senseType, int range)
        {
            Name = name;
            SenseType = senseType;
            Range = range;
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            if (creature.Senses.TryGetValue(SenseType, out int currentRange))
            {
                // Use the greater range
                if (currentRange < Range)
                {
                    creature.Senses[SenseType] = Range;
                }
            }
            else
            {
                creature.Senses[SenseType] = Range;
            }
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            // Removing is tricky if multiple sources provide the same sense.
            // For now, we'll just remove it, but a robust system would track sources.
            if (creature.Senses.ContainsKey(SenseType))
            {
                creature.Senses.Remove(SenseType);
            }
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack) { }
        public void OnStartTurn(ICreature creature) { }
    }
}
