using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Dice;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardCheckManager : ICheckManager, IStateful<CheckManagerState>
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

        /// <summary>
        /// Restores a check manager from previously-saved proficiency
        /// state — see <see cref="CheckManagerState"/>'s own doc comment
        /// for the round-trip bug this constructor closes. <paramref name="state"/>
        /// may be null (a save predating this field, or a creature with no
        /// proficiencies recorded), in which case this behaves exactly
        /// like the no-state constructor above.
        /// </summary>
        public StandardCheckManager(IDiceRoller diceRoller, ICreature creature, CheckManagerState? state)
            : this(diceRoller, creature)
        {
            if (state is null) return;
            foreach (var skill in state.SkillProficiencies)
            {
                AddSkillProficiency(skill);
            }
            foreach (var ability in state.SavingThrowProficiencies)
            {
                AddSavingThrowProficiency(ability);
            }
        }

        public CheckManagerState GetState() => new(
            new Collection<string>(_skillProficiencies.ToList()),
            new Collection<Ability>(_savingThrowProficiencies.ToList()));

        public Result<DiceRollResult> RollAbilityCheck(Ability ability, string? skillName = null)
        {
            int modifier = _creature.AbilityScores.GetModifier(ability);
            int proficiencyBonus = 0;

            if (!string.IsNullOrWhiteSpace(skillName) && HasSkillProficiency(skillName))
            {
                proficiencyBonus = _creature.ProficiencyBonus;
            }

            var roll = _diceRoller.Roll(DiceNotation.WithModifier("1d20", modifier + proficiencyBonus));

            if (!roll.IsSuccess) return Result<DiceRollResult>.Failure(roll.Error);

            int total = roll.Value.Total;
            if (_creature.Effects != null)
            {
                // skillName is forwarded here (not just used for the
                // proficiency lookup above) so a skill-scoped StatBonusEffect
                // (e.g. "+2 Intimidation") can tell this check apart from a
                // bare ability check or a different skill under the same
                // ability — see StatBonusEffect.ModifyStat.
                total = _creature.Effects.ApplyStatBonuses(StatType.AbilityCheck, total, skillName);
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

            var roll = _diceRoller.Roll(DiceNotation.WithModifier("1d20", modifier + proficiencyBonus));

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
