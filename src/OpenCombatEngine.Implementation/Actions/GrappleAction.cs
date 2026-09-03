// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Conditions;

namespace OpenCombatEngine.Implementation.Actions
{
    /// <summary>
    /// SRD Grapple: a melee-range opposed check — source's Strength
    /// (Athletics) against the better of the target's own Strength
    /// (Athletics) or Dexterity (Acrobatics) — applying
    /// <see cref="ConditionType.Grappled"/> to the target on success.
    /// Requires a free hand (SRD rule), approximated from real equipped-
    /// weapon data: <c>Equipment.OffHand</c>/<c>Shield</c> must be empty
    /// and <c>MainHand</c> must not be a <see cref="WeaponProperty.TwoHanded"/>
    /// weapon, since this engine has no other "is a hand free" concept.
    /// Creature Size (SRD's "no more than one size category larger"
    /// restriction) is not modeled anywhere in this engine and is
    /// deliberately not checked here — a documented simplification, not
    /// an oversight.
    /// </summary>
    public class GrappleAction : IAction
    {
        public string Name => "Grapple";
        public string Description => "Attempt to grab and restrain a creature within reach.";
        public ActionType Type { get; }
        public int Range { get; }

        private readonly IDiceRoller _diceRoller;

        public GrappleAction(IDiceRoller diceRoller, ActionType type = ActionType.Action, int range = 5)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            Type = type;
            Range = range;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var source = context.Source;

            if (IncapacitationCheck.BlockingCondition(source) is { } blockingCondition)
            {
                return Result<ActionResult>.Failure($"{source.Name} is {blockingCondition} and cannot act.");
            }

            if (context.Target is not CreatureTarget creatureTarget)
            {
                return Result<ActionResult>.Failure("Target must be a creature to grapple.");
            }
            var target = creatureTarget.Creature;

            if (context.Grid != null)
            {
                var sourcePos = context.Grid.GetPosition(source);
                var targetPos = context.Grid.GetPosition(target);
                if (sourcePos == null) return Result<ActionResult>.Failure("Grappler is not on the grid.");
                if (targetPos == null) return Result<ActionResult>.Failure("Target is not on the grid.");

                var distance = context.Grid.GetDistance(sourcePos.Value, targetPos.Value);
                if (distance > Range)
                {
                    return Result<ActionResult>.Failure($"Target is out of reach. Distance: {distance}, Reach: {Range}");
                }
                if (!context.Grid.HasLineOfSight(sourcePos.Value, targetPos.Value))
                {
                    return Result<ActionResult>.Failure("No line of sight to target.");
                }
            }

            if (source.ActionEconomy != null && !context.BypassActionEconomy)
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
            }

            bool offHandOccupied = source.Equipment?.OffHand != null || source.Equipment?.Shield != null;
            bool mainHandTwoHanded = source.Equipment?.MainHand?.Properties.Contains(WeaponProperty.TwoHanded) ?? false;
            if (offHandOccupied || mainHandTwoHanded)
            {
                return Result<ActionResult>.Failure($"{source.Name} has no hand free to grapple.");
            }

            if (source.ActionEconomy != null && !context.BypassActionEconomy)
            {
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

            var sourceRoll = source.Checks.RollAbilityCheck(Ability.Strength, "Athletics");
            if (!sourceRoll.IsSuccess)
            {
                return Result<ActionResult>.Failure($"Failed to roll grapple check: {sourceRoll.Error}");
            }

            var targetAthletics = target.Checks.RollAbilityCheck(Ability.Strength, "Athletics");
            var targetAcrobatics = target.Checks.RollAbilityCheck(Ability.Dexterity, "Acrobatics");
            int targetBest = int.MinValue;
            if (targetAthletics.IsSuccess) targetBest = Math.Max(targetBest, targetAthletics.Value.Total);
            if (targetAcrobatics.IsSuccess) targetBest = Math.Max(targetBest, targetAcrobatics.Value.Total);

            bool succeeded = sourceRoll.Value.Total >= targetBest;
            if (succeeded)
            {
                // -1 is this codebase's own "permanent, until removed"
                // duration convention (StandardConditionManager.Tick only
                // decrements/removes DurationRounds >= 0) — Grappled ends
                // when the grappler is incapacitated or the target
                // escapes/is moved away, not after a fixed round count.
                var condition = ConditionFactory.Create(ConditionType.Grappled, -1, target);
                if (condition != null)
                {
                    target.Conditions.AddCondition(condition);
                }
                return Result<ActionResult>.Success(new ActionResult(true, $"{source.Name} grapples {target.Name} ({sourceRoll.Value.Total} vs {targetBest})."));
            }

            return Result<ActionResult>.Success(new ActionResult(false, $"{source.Name} fails to grapple {target.Name} ({sourceRoll.Value.Total} vs {targetBest})."));
        }
    }
}
