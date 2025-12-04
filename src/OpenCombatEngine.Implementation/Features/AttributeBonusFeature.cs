using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Implementation.Effects;

namespace OpenCombatEngine.Implementation.Features
{
    public class AttributeBonusFeature : IFeature
    {
        public string Name { get; }
        public string AttributeName { get; }
        public int Bonus { get; }

        public AttributeBonusFeature(string name, string attributeName, int bonus)
        {
            Name = name;
            AttributeName = attributeName;
            Bonus = bonus;
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            // Map attribute name to StatType
            if (Enum.TryParse(AttributeName, true, out StatType statType))
            {
                var effect = new StatBonusEffect(
                    Name,
                    $"Adds {Bonus} to {AttributeName}",
                    -1, // Permanent
                    statType,
                    Bonus
                );
                creature.Effects.AddEffect(effect);
            }
            else
            {
                // Log warning or ignore? For now ignore.
            }
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            creature.Effects.RemoveEffect(Name);
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack) { }
        public void OnStartTurn(ICreature creature) { }
    }
}
