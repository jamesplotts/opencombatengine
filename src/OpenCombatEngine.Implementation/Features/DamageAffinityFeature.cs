using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    public enum AffinityType
    {
        Resistance,
        Vulnerability,
        Immunity
    }

    public class DamageAffinityFeature : IFeature
    {
        public string Name { get; }
        public DamageType DamageType { get; }
        public AffinityType AffinityType { get; }

        public DamageAffinityFeature(string name, DamageType damageType, AffinityType affinityType)
        {
            Name = name;
            DamageType = damageType;
            AffinityType = affinityType;
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            switch (AffinityType)
            {
                case AffinityType.Resistance:
                    creature.CombatStats.AddResistance(DamageType);
                    break;
                case AffinityType.Vulnerability:
                    creature.CombatStats.AddVulnerability(DamageType);
                    break;
                case AffinityType.Immunity:
                    creature.CombatStats.AddImmunity(DamageType);
                    break;
            }
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            switch (AffinityType)
            {
                case AffinityType.Resistance:
                    creature.CombatStats.RemoveResistance(DamageType);
                    break;
                case AffinityType.Vulnerability:
                    creature.CombatStats.RemoveVulnerability(DamageType);
                    break;
                case AffinityType.Immunity:
                    creature.CombatStats.RemoveImmunity(DamageType);
                    break;
            }
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack) { }
        public void OnStartTurn(ICreature creature) { }
    }
}
