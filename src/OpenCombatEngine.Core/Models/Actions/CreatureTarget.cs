using System;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Core.Models.Actions
{
    public class CreatureTarget : IActionTarget
    {
        public ICreature Creature { get; }
        public string Description => $"Creature: {Creature.Name}";

        public CreatureTarget(ICreature creature)
        {
            Creature = creature ?? throw new ArgumentNullException(nameof(creature));
        }
    }
}
