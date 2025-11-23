using System;
using System.Globalization;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Events;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Actions
{
    public class MonsterAttackAction : IAction
    {
        public string Name { get; }
        public string Description { get; }
        public ActionType Type => ActionType.Action;
        public int Range { get; }

        public int ToHitBonus { get; }
        public string DamageDice { get; } // e.g. "1d6+2"
        public DamageType DamageType { get; }

        public MonsterAttackAction(string name, string description, int toHitBonus, string damageDice, DamageType damageType, int range = 5)
        {
            Name = name;
            Description = description;
            ToHitBonus = toHitBonus;
            DamageDice = damageDice;
            DamageType = damageType;
            Range = range;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            if (context == null) return Result<ActionResult>.Failure("Context is null.");
            var source = context.Source;
            
            if (context.Target is not OpenCombatEngine.Core.Models.Actions.CreatureTarget creatureTarget)
            {
                return Result<ActionResult>.Failure("Target must be a creature.");
            }
            var target = creatureTarget.Creature;

            // Range Validation
            if (context.Grid != null)
            {
                var sourcePos = context.Grid.GetPosition(source);
                var targetPos = context.Grid.GetPosition(target);

                if (sourcePos == null) return Result<ActionResult>.Failure("Attacker is not on the grid.");
                if (targetPos == null) return Result<ActionResult>.Failure("Target is not on the grid.");

                var distance = context.Grid.GetDistance(sourcePos.Value, targetPos.Value);
                if (distance > Range)
                {
                    return Result<ActionResult>.Failure($"Target is out of range. Distance: {distance}, Range: {Range}");
                }
            }

            // 1. Check Action Economy
            if (!source.ActionEconomy.HasAction)
            {
                return Result<ActionResult>.Failure("No action available.");
            }

            // 2. Roll to Hit
#pragma warning disable CA5394 // Random is an insecure random number generator
            var d20 = new Random().Next(1, 21);
#pragma warning restore CA5394
            var attackRoll = d20 + ToHitBonus;
            bool isCrit = d20 == 20; // Simple crit check

            // 3. Roll Damage
            var damage = ParseAndRollDamage(DamageDice);
            if (isCrit)
            {
                // Roll again for crit
                damage += ParseAndRollDamage(DamageDice);
            }

            // 4. Create AttackResult
            var damageRolls = new System.Collections.Generic.List<OpenCombatEngine.Core.Models.Combat.DamageRoll>
            {
                new OpenCombatEngine.Core.Models.Combat.DamageRoll(damage, DamageType)
            };

            var attackResult = new OpenCombatEngine.Core.Models.Combat.AttackResult(
                source,
                target,
                attackRoll,
                isCrit,
                false, // HasAdvantage
                false, // HasDisadvantage
                damageRolls
            );
            
            // 4.5 Modify Outgoing Attack
            source.ModifyOutgoingAttack(attackResult);

            // 5. Resolve
            var outcome = target.ResolveAttack(attackResult);

            return Result<ActionResult>.Success(new ActionResult(outcome.IsHit, outcome.Message, outcome.DamageDealt));
        }

        private static int ParseAndRollDamage(string diceString)
        {
            // Simple parser for "1d6+2" or "1d6" or "5"
            try 
            {
                var parts = diceString.ToUpperInvariant().Split('+');
                var dicePart = parts[0].Trim();
                var bonus = parts.Length > 1 ? int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) : 0;

                if (dicePart.Contains('d', StringComparison.OrdinalIgnoreCase))
                {
                    var dParts = dicePart.Split('d');
                    var count = int.Parse(dParts[0], CultureInfo.InvariantCulture);
                    var sides = int.Parse(dParts[1], CultureInfo.InvariantCulture);
                    
                    var total = 0;
#pragma warning disable CA5394 // Random is an insecure random number generator
                    var rng = new Random();
                    for(int i=0; i<count; i++)
                    {
                        total += rng.Next(1, sides + 1);
                    }
#pragma warning restore CA5394
                    return total + bonus;
                }
                else
                {
                    return int.Parse(dicePart, CultureInfo.InvariantCulture) + bonus;
                }
            }
            catch (FormatException)
            {
                return 1;
            }
            catch (OverflowException)
            {
                return 1;
            }
        }
    }
}
