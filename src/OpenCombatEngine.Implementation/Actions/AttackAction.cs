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
            string attackNotation = $"1d20+{_attackBonus}";
            
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
            bool isCrit = attackRollResult.Value.IsCriticalSuccess;

            // 2. Roll Damage
            string damageDice = _damageDice;
            DamageType damageType = _damageType;
            int damageBonus = _damageBonus;

            if (Name == "Attack" && source.Equipment?.MainHand != null)
            {
                damageDice = source.Equipment.MainHand.DamageDice;
                damageType = source.Equipment.MainHand.DamageType;
            }
            
            var damageRollResult = _diceRoller.Roll(damageDice);
            if (!damageRollResult.IsSuccess)
            {
                 return Result<ActionResult>.Failure($"Failed to roll damage: {damageRollResult.Error}");
            }
            int damageAmount = damageRollResult.Value.Total + damageBonus;
            if (damageAmount < 0) damageAmount = 0;

            if (isCrit)
            {
                var critRollResult = _diceRoller.Roll(damageDice);
                if (!critRollResult.IsSuccess)
                {
                    return Result<ActionResult>.Failure($"Failed to roll critical damage: {critRollResult.Error}");
                }
                damageAmount += critRollResult.Value.Total;
            }

            // 3. Create AttackResult
            var damageRolls = new System.Collections.Generic.List<OpenCombatEngine.Core.Models.Combat.DamageRoll>
            {
                new OpenCombatEngine.Core.Models.Combat.DamageRoll(damageAmount, damageType)
            };

            var attackResult = new OpenCombatEngine.Core.Models.Combat.AttackResult(
                source,
                target,
                attackTotal,
                isCrit,
                damageRolls
            );

            // 4. Resolve
            var outcome = target.ResolveAttack(attackResult);

            return Result<ActionResult>.Success(new ActionResult(outcome.IsHit, outcome.Message, outcome.DamageDealt));
        }
    }
}
