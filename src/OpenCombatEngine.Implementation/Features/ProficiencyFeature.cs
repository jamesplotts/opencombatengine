using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    public class ProficiencyFeature : IFeature
    {
        public string Name { get; }
        public string? SkillName { get; }
        public Ability? SavingThrowAbility { get; }

        public ProficiencyFeature(string name, string skillName)
        {
            Name = name;
            SkillName = skillName;
        }

        public ProficiencyFeature(string name, Ability savingThrowAbility)
        {
            Name = name;
            SavingThrowAbility = savingThrowAbility;
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            
            if (!string.IsNullOrEmpty(SkillName))
            {
                creature.Checks.AddSkillProficiency(SkillName);
            }
            
            if (SavingThrowAbility.HasValue)
            {
                creature.Checks.AddSavingThrowProficiency(SavingThrowAbility.Value);
            }
        }

        public void OnRemoved(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);
            
            if (!string.IsNullOrEmpty(SkillName))
            {
                creature.Checks.RemoveSkillProficiency(SkillName);
            }
            
            if (SavingThrowAbility.HasValue)
            {
                creature.Checks.RemoveSavingThrowProficiency(SavingThrowAbility.Value);
            }
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack) { }
        public void OnStartTurn(ICreature creature) { }
    }
}
