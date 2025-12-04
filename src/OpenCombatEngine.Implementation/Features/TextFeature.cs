using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    /// <summary>
    /// A simple feature that holds a name and description but has no active logic.
    /// Used for features imported from content that don't have specific implementations yet.
    /// </summary>
    public class TextFeature : IFeature
    {
        public string Name { get; }
        public string Description { get; }

        public TextFeature(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack)
        {
            // No operation
        }

        public void OnApplied(ICreature creature)
        {
            // No operation
        }

        public void OnRemoved(ICreature creature)
        {
            // No operation
        }

        public void OnStartTurn(ICreature creature)
        {
            // No operation
        }
    }
}
