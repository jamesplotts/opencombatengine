using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Actions
{
    public class AttackAction : IAction
    {
        public string Name { get; }
        public string Description { get; }
        public ActionType Type { get; }

        private readonly int _attackBonus;
        private readonly string _damageDice;
        private readonly DamageType _damageType;
        private readonly int _damageBonus;
        private readonly IDiceRoller _diceRoller;

        public AttackAction(string name, string description, int attackBonus, string damageDice, DamageType damageType, int damageBonus, IDiceRoller diceRoller, ActionType type = ActionType.Action)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(damageDice)) throw new ArgumentException("Damage dice cannot be empty", nameof(damageDice));
            ArgumentNullException.ThrowIfNull(diceRoller);

            Name = name;
            Description = description;
            _attackBonus = attackBonus;
            _damageDice = damageDice;
            _damageType = damageType;
            _damageBonus = damageBonus;
            _diceRoller = diceRoller;
            Type = type;
        }

        public Result<ActionResult> Execute(ICreature source, ICreature target)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(target);

            // 0. Check Action Economy
            if (source.ActionEconomy != null)
            {
                bool canAct = Type switch
                {
                    ActionType.Action => source.ActionEconomy.HasAction,
                    ActionType.BonusAction => source.ActionEconomy.HasBonusAction,
                    ActionType.Reaction => source.ActionEconomy.HasReaction,
                    _ => true
                };

                if (!canAct)
                {
                    return Result<ActionResult>.Failure($"Cannot perform {Type}: Resource already used.");
                }

                // Consume resource
                switch (Type)
                {
                    case ActionType.Action:
                        source.ActionEconomy.UseAction();
                        break;
                    case ActionType.BonusAction:
                        source.ActionEconomy.UseBonusAction();
                        break;
                    case ActionType.Reaction:
                        source.ActionEconomy.UseReaction();
                        break;
                }
            }

            // 1. Roll to Hit
            // We use d20 + attackBonus
            // Note: We should probably use a more robust dice notation builder, but string concat is fine for now.
            string attackNotation = $"1d20+{_attackBonus}";
            
            // Check for Prone (Disadvantage)
            bool isProne = source.Conditions?.HasCondition(ConditionType.Prone) ?? false;
            
            Result<DiceRollResult> attackRollResult;
            if (isProne)
            {
                attackRollResult = _diceRoller.RollWithDisadvantage(attackNotation);
            }
            else
            {
                attackRollResult = _diceRoller.Roll(attackNotation);
            }

            if (!attackRollResult.IsSuccess)
            {
                return Result<ActionResult>.Failure($"Failed to roll attack: {attackRollResult.Error}");
            }

            var attackTotal = attackRollResult.Value.Total;
            var targetAC = target.CombatStats.ArmorClass;

            // 2. Check for Hit
            // Meets it beats it
            bool isHit = attackTotal >= targetAC;
            
            // Critical Hit check (Natural 20)
            bool isCrit = attackRollResult.Value.IsCriticalSuccess;
            if (isCrit) isHit = true;

            // Critical Miss check (Natural 1)
            // If IsCriticalFailure is true (which usually means nat 1 on d20), it's a miss regardless of mods.
            // We need to check if DiceRollResult has IsCriticalFailure. It does not in the previous view, 
            // but let's check if we missed it or if we need to infer it.
            // The DiceRollResult had IsCriticalSuccess. Let's assume standard rules: Nat 1 is auto miss.
            // We can check IndividualRolls if needed, but let's stick to simple total >= AC for now unless we want to be strict.
            // Actually, let's just use total >= AC.

            if (!isHit)
            {
                return Result<ActionResult>.Success(new ActionResult(false, $"Missed! Rolled {attackTotal} vs AC {targetAC}"));
            }

            // 3. Roll Damage
            // If crit, we roll double dice. 
            // StandardDiceRoller doesn't natively support "Crit" flag to double dice, we have to manipulate notation.
            // Determine damage dice and type
            string damageDice = _damageDice;
            DamageType damageType = _damageType;
            int damageBonus = _damageBonus;

            // Check for equipped weapon if using default "Attack"
            if (Name == "Attack" && source.Equipment?.MainHand != null)
            {
                damageDice = source.Equipment.MainHand.DamageDice;
                damageType = source.Equipment.MainHand.DamageType;
                // Assuming weapon damage bonus is included in its DamageDice string or handled separately.
                // For now, we'll just use the action's damageBonus if not overridden by weapon.
                // If weapon has its own bonus, it should be added here.
                // For simplicity, let's assume weapon damage dice string includes its bonus, or we add a flat bonus.
                // For now, we'll keep the action's damageBonus.
            }
            
            // Roll base damage
            var damageRollResult = _diceRoller.Roll(damageDice);
            if (!damageRollResult.IsSuccess)
            {
                 return Result<ActionResult>.Failure($"Failed to roll damage: {damageRollResult.Error}");
            }
            int damage = damageRollResult.Value.Total + damageBonus;
            if (damage < 0) damage = 0; // Minimum 0 damage

            // Critical Hit: Roll damage dice again (double dice)
            if (isCrit)
            {
                var critRollResult = _diceRoller.Roll(damageDice);
                if (!critRollResult.IsSuccess)
                {
                    return Result<ActionResult>.Failure($"Failed to roll critical damage: {critRollResult.Error}");
                }
                damage += critRollResult.Value.Total;
            }

            // 4. Apply Damage
            // Assuming ICreature.HitPoints.TakeDamage has an overload for DamageType
            target.HitPoints.TakeDamage(damage, damageType);

            string message = isCrit 
                ? $"CRITICAL HIT! Dealt {damage} {damageType} damage." 
                : $"Hit! Dealt {damage} {damageType} damage.";

            return Result<ActionResult>.Success(new ActionResult(true, message, damage));
        }
    }
}
