using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Spatial.Shapes;

namespace OpenCombatEngine.Implementation.Actions.Spells
{
    public class FireballAction : IAction
    {
        public string Name { get; }
        public string Description { get; }
        public ActionType Type { get; } = ActionType.Action;
        public int Range { get; } = 150; // Fireball range 150ft
        public int Radius { get; } = 20; // 20ft radius
        public int DC { get; }
        public string DamageDice { get; } = "8d6";

        private readonly IDiceRoller _diceRoller;

        public FireballAction(string name, string description, int dc, IDiceRoller diceRoller)
        {
            Name = name;
            Description = description;
            DC = dc;
            _diceRoller = diceRoller;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.Target is not PositionTarget positionTarget)
            {
                return Result<ActionResult>.Failure("Fireball requires a position target.");
            }

            if (context.Grid == null)
            {
                return Result<ActionResult>.Failure("Fireball requires a grid.");
            }

            var origin = positionTarget.Position;

            // Range Check
            var casterPos = context.Grid.GetPosition(context.Source);
            if (casterPos == null) return Result<ActionResult>.Failure("Caster not on grid.");

            if (context.Grid.GetDistance(casterPos.Value, origin) > Range)
            {
                return Result<ActionResult>.Failure($"Target out of range ({Range}ft).");
            }

            // Roll Damage
            var damageRoll = _diceRoller.Roll(DamageDice);
            if (!damageRoll.IsSuccess) return Result<ActionResult>.Failure($"Failed to roll damage: {damageRoll.Error}");
            
            int totalDamage = damageRoll.Value.Total;

            // Get Targets (Radius 20ft = 4 squares)
            var shape = new SphereShape(Radius / 5);
            var victims = context.Grid.GetCreaturesInShape(origin, shape, null).ToList();

            string message = $"Fireball explodes at {origin}. Hit {victims.Count} creatures. Damage: {totalDamage} (Fire).";

            foreach (var victim in victims)
            {
                // Saving Throw (Dexterity)
                var save = victim.Checks.RollSavingThrow(Ability.Dexterity);
                int damageToTake = totalDamage;
                if (save.IsSuccess)
                {
                    if (save.Value >= DC)
                    {
                        damageToTake /= 2;
                    }
                }

                // Apply Damage
                victim.HitPoints.TakeDamage(damageToTake, DamageType.Fire);
                
                // Note: We could log individual results here or append to message.
            }

            return Result<ActionResult>.Success(new ActionResult(true, message, totalDamage));
        }
    }
}
