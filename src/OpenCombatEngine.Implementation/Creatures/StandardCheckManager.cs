using System;
using System.Collections.Generic;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardCheckManager : ICheckManager
    {
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _creature; // To access ProficiencyBonus if needed, or we pass it in.
        // Actually, we need ProficiencyBonus for saves.
        // Circular dependency risk if we pass ICreature into CheckManager and CheckManager is on ICreature.
        // But StandardCreature constructs it, so it can pass 'this'.
        
        private readonly HashSet<string> _skillProficiencies = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Ability> _savingThrowProficiencies = new();

        public StandardCheckManager(IDiceRoller diceRoller, ICreature creature)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            _creature = creature ?? throw new ArgumentNullException(nameof(creature));
        }

        public Result<DiceRollResult> RollAbilityCheck(Ability ability, string? skillName = null)
        {
            int modifier = _creature.AbilityScores.GetModifier(ability);
            int proficiencyBonus = 0;

            if (!string.IsNullOrWhiteSpace(skillName) && HasSkillProficiency(skillName))
            {
                proficiencyBonus = _creature.ProficiencyBonus;
            }

            var roll = _diceRoller.Roll($"1d20+{modifier + proficiencyBonus}");

            if (!roll.IsSuccess) return Result<DiceRollResult>.Failure(roll.Error);

            int total = roll.Value.Total;
            if (_creature.Effects != null)
            {
                total = _creature.Effects.ApplyStatBonuses(StatType.AbilityCheck, total);
            }

            // Effects may have adjusted the total beyond what the raw roll
            // produced; IndividualRolls/Notation/Modifier/RollType still
            // describe the actual die(s) rolled, so only Total is replaced.
            return Result<DiceRollResult>.Success(roll.Value with { Total = total });
        }

        public Result<DiceRollResult> RollSavingThrow(Ability ability)
        {
            int modifier = _creature.AbilityScores.GetModifier(ability);
            int proficiencyBonus = 0;

            if (HasSavingThrowProficiency(ability))
            {
                proficiencyBonus = _creature.ProficiencyBonus;
            }

            var roll = _diceRoller.Roll($"1d20+{modifier + proficiencyBonus}");

            if (!roll.IsSuccess) return Result<DiceRollResult>.Failure(roll.Error);

            int total = roll.Value.Total;
            if (_creature.Effects != null)
            {
                total = _creature.Effects.ApplyStatBonuses(StatType.SavingThrow, total);
            }

            SavingThrowRolled?.Invoke(this, new OpenCombatEngine.Core.Models.Events.SavingThrowEventArgs(ability, total, _creature));

            return Result<DiceRollResult>.Success(roll.Value with { Total = total });
        }

        /// <inheritdoc />
        public event EventHandler<OpenCombatEngine.Core.Models.Events.SavingThrowEventArgs>? SavingThrowRolled;

        public Result<DiceRollResult> RollDeathSave()
        {
            var roll = _diceRoller.Roll("1d20");
            if (!roll.IsSuccess) return Result<DiceRollResult>.Failure(roll.Error);
            return Result<DiceRollResult>.Success(roll.Value);
        }

        public void AddSkillProficiency(string skillName)
        {
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                _skillProficiencies.Add(skillName);
            }
        }

        public void RemoveSkillProficiency(string skillName)
        {
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                _skillProficiencies.Remove(skillName);
            }
        }

        public bool HasSkillProficiency(string skillName)
        {
            return !string.IsNullOrWhiteSpace(skillName) && _skillProficiencies.Contains(skillName);
        }

        public void AddSavingThrowProficiency(Ability ability)
        {
            _savingThrowProficiencies.Add(ability);
        }

        public void RemoveSavingThrowProficiency(Ability ability)
        {
            _savingThrowProficiencies.Remove(ability);
        }

        public bool HasSavingThrowProficiency(Ability ability)
        {
            return _savingThrowProficiencies.Contains(ability);
        }
    }
}
