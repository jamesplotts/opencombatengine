using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    public class SpellcastingFeature : IFeature
    {
        public string Name { get; }
        public IReadOnlyList<ISpell> Spells { get; }

        public SpellcastingFeature(string name, IEnumerable<ISpell> spells)
        {
            Name = name;
            Spells = new List<ISpell>(spells).AsReadOnly();
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            if (creature.Spellcasting != null)
            {
                foreach (var spell in Spells)
                {
                    creature.Spellcasting.LearnSpell(spell);
                }
            }
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            if (creature.Spellcasting != null)
            {
                foreach (var spell in Spells)
                {
                    creature.Spellcasting.UnlearnSpell(spell);
                }
            }
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack) { }
        public void OnStartTurn(ICreature creature) { }
    }
}
