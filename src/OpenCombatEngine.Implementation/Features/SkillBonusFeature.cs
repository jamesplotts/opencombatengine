using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Models.Combat;

namespace OpenCombatEngine.Implementation.Features
{
    /// <summary>
    /// Grants a flat bonus to one named skill's checks only (e.g. "+2 to
    /// Intimidation checks") — the skill-scoped sibling of
    /// <see cref="StatBonusFeature"/>, whose <c>Dictionary&lt;StatType, int&gt;</c>
    /// shape has no room for a bonus that applies to one skill but not
    /// every ability check. A magic item or feat that should boost a
    /// specific skill attaches one <see cref="SkillBonusFeature"/> per
    /// skill, the same way race/class content attaches one
    /// <see cref="ProficiencyFeature"/> per granted proficiency.
    /// </summary>
    public class SkillBonusFeature : IFeature
    {
        public string Name { get; }
        public string Description { get; }
        public string SkillName { get; }
        public int Bonus { get; }

        private string? _appliedEffectName;

        public SkillBonusFeature(string name, string description, string skillName, int bonus)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(skillName)) throw new ArgumentException("Skill name cannot be empty", nameof(skillName));
            Name = name;
            Description = description;
            SkillName = skillName;
            Bonus = bonus;
        }

        public void OnApplied(ICreature creature)
        {
            ArgumentNullException.ThrowIfNull(creature);

            _appliedEffectName = $"{Name}_{SkillName}";
            var effect = new OpenCombatEngine.Implementation.Effects.StatBonusEffect(
                _appliedEffectName,
                $"Bonus from {Name}",
                -1, // Permanent
                StatType.AbilityCheck,
                Bonus,
                targetSkillName: SkillName
            );

            creature.Effects.AddEffect(effect);
        }

        public void OnRemoved(ICreature creature)
        {
            if (creature == null || _appliedEffectName == null) return;

            creature.Effects.RemoveEffect(_appliedEffectName);
            _appliedEffectName = null;
        }

        public void OnOutgoingAttack(ICreature source, AttackResult attack)
        {
            // Handled via Effects system
        }

        public void OnStartTurn(ICreature creature)
        {
            // No turn start logic needed for static bonuses
        }
    }
}
