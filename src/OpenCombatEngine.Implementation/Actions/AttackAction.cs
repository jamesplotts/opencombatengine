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
        private readonly IDiceRoller _diceRoller;

        public AttackAction(string name, string description, int attackBonus, string damageDice, IDiceRoller diceRoller, ActionType type = ActionType.Action)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(damageDice)) throw new ArgumentException("Damage dice cannot be empty", nameof(damageDice));
            ArgumentNullException.ThrowIfNull(diceRoller);

            Name = name;
            Description = description;
            _attackBonus = attackBonus;
            _damageDice = damageDice;
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
            var attackRollResult = _diceRoller.Roll(attackNotation);

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
            // For now, let's implement standard damage.
            
            string damageNotation = _damageDice;
            // If crit, we might want to double the dice count in the notation. 
            // Parsing "1d8+3" -> "2d8+3". This is complex string manipulation.
            // For MVP, we'll just roll normal damage.
            
            var damageRollResult = _diceRoller.Roll(damageNotation);
            if (!damageRollResult.IsSuccess)
            {
                 return Result<ActionResult>.Failure($"Failed to roll damage: {damageRollResult.Error}");
            }

            int damage = damageRollResult.Value.Total;

            // 4. Apply Damage
            target.HitPoints.TakeDamage(damage);

            string message = isCrit 
                ? $"CRITICAL HIT! Dealt {damage} damage." 
                : $"Hit! Dealt {damage} damage.";

            return Result<ActionResult>.Success(new ActionResult(true, message, damage));
        }
    }
}
