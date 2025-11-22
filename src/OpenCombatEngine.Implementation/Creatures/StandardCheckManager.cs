using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardCheckManager : ICheckManager
    {
        private readonly IAbilityScores _abilityScores;
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _creature; // To access ProficiencyBonus if needed, or we pass it in.
        // Actually, we need ProficiencyBonus for saves.
        // Circular dependency risk if we pass ICreature into CheckManager and CheckManager is on ICreature.
        // But StandardCreature constructs it, so it can pass 'this'.
        
        public StandardCheckManager(IAbilityScores abilityScores, IDiceRoller diceRoller, ICreature creature)
        {
            _abilityScores = abilityScores ?? throw new ArgumentNullException(nameof(abilityScores));
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            _creature = creature ?? throw new ArgumentNullException(nameof(creature));
        }

        public Result<int> RollAbilityCheck(Ability ability)
        {
            int modifier = _abilityScores.GetModifier(ability);
            var roll = _diceRoller.Roll($"1d20+{modifier}");
            
            if (!roll.IsSuccess) return Result<int>.Failure(roll.Error);
            
            return Result<int>.Success(roll.Value.Total);
        }

        public Result<int> RollSavingThrow(Ability ability)
        {
            int modifier = _abilityScores.GetModifier(ability);
            
            // TODO: Check for proficiency. For now, we assume no proficiency or we need a way to check.
            // The plan said: "RollSavingThrow just rolls d20 + Mod + (IsProficient ? PB : 0)."
            // But we didn't add IsProficient to ICreature yet.
            // Let's assume 0 proficiency for now to keep it simple as per plan "assume no proficiency by default".
            // Or better, let's just use the modifier.
            
            int proficiencyBonus = 0; 
            // Future: if (_creature.SavingThrowProficiencies.Contains(ability)) proficiencyBonus = _creature.ProficiencyBonus;

            var roll = _diceRoller.Roll($"1d20+{modifier + proficiencyBonus}");

            if (!roll.IsSuccess) return Result<int>.Failure(roll.Error);

            return Result<int>.Success(roll.Value.Total);
        }

        public Result<int> RollDeathSave()
        {
            var roll = _diceRoller.Roll("1d20");
            if (!roll.IsSuccess) return Result<int>.Failure(roll.Error);
            return Result<int>.Success(roll.Value.Total);
        }
    }
}
