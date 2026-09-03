// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Conditions;

namespace OpenCombatEngine.Implementation.Actions
{
    /// <summary>
    /// SRD Shove: a melee-range opposed check — source's Strength
    /// (Athletics) against the better of the target's own Strength
    /// (Athletics) or Dexterity (Acrobatics) — that on success either
    /// knocks the target <see cref="ConditionType.Prone"/> or pushes it
    /// one grid cell (5 feet, this engine's own square-size convention)
    /// directly away from the source, per <see cref="Effect"/>. No free
    /// hand is required (unlike <see cref="GrappleAction"/>). Creature
    /// Size is not modeled anywhere in this engine and is deliberately
    /// not checked here, same simplification as Grapple. The push has no
    /// spatial effect when no grid exists (the contest still resolves
    /// and is reported) — same "no grid, no spatial effect" rule this
    /// contract already applies elsewhere.
    /// </summary>
    public class ShoveAction : IAction
    {
        public string Name => "Shove";
        public string Description => "Attempt to knock a creature prone or push it away.";
        public ActionType Type { get; }
        public int Range { get; }
        public ShoveEffect Effect { get; }

        private readonly IDiceRoller _diceRoller;

        public ShoveAction(IDiceRoller diceRoller, ShoveEffect effect, ActionType type = ActionType.Action, int range = 5)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            Effect = effect;
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
                return Result<ActionResult>.Failure("Target must be a creature to shove.");
            }
            var target = creatureTarget.Creature;

            Position? sourcePos = null;
            Position? targetPos = null;
            if (context.Grid != null)
            {
                sourcePos = context.Grid.GetPosition(source);
                targetPos = context.Grid.GetPosition(target);
                if (sourcePos == null) return Result<ActionResult>.Failure("Shover is not on the grid.");
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
                return Result<ActionResult>.Failure($"Failed to roll shove check: {sourceRoll.Error}");
            }

            var targetAthletics = target.Checks.RollAbilityCheck(Ability.Strength, "Athletics");
            var targetAcrobatics = target.Checks.RollAbilityCheck(Ability.Dexterity, "Acrobatics");
            int targetBest = int.MinValue;
            if (targetAthletics.IsSuccess) targetBest = Math.Max(targetBest, targetAthletics.Value.Total);
            if (targetAcrobatics.IsSuccess) targetBest = Math.Max(targetBest, targetAcrobatics.Value.Total);

            bool succeeded = sourceRoll.Value.Total >= targetBest;
            if (!succeeded)
            {
                return Result<ActionResult>.Success(new ActionResult(false, $"{source.Name} fails to shove {target.Name} ({sourceRoll.Value.Total} vs {targetBest})."));
            }

            if (Effect == ShoveEffect.Prone)
            {
                // -1 is this codebase's "permanent, until removed"
                // duration convention (see GrappleAction's own remark) —
                // Prone ends when the creature spends movement to stand
                // up, not after a fixed round count, so 0 (which
                // StandardConditionManager.Tick would strip on this
                // creature's very next turn) would be wrong.
                var condition = ConditionFactory.Create(ConditionType.Prone, -1, target);
                if (condition != null)
                {
                    target.Conditions.AddCondition(condition);
                }
                return Result<ActionResult>.Success(new ActionResult(true, $"{source.Name} shoves {target.Name} prone ({sourceRoll.Value.Total} vs {targetBest})."));
            }

            // Push: move target one cell (5 feet) directly away from
            // source along the same line between them. No-op position-
            // wise without a grid — the contest still resolves.
            if (context.Grid != null && sourcePos != null && targetPos != null)
            {
                int dx = Math.Sign(targetPos.Value.X - sourcePos.Value.X);
                int dy = Math.Sign(targetPos.Value.Y - sourcePos.Value.Y);
                var pushedPosition = new Position(targetPos.Value.X + dx, targetPos.Value.Y + dy, targetPos.Value.Z);
                context.Grid.MoveCreature(target, pushedPosition);
            }
            return Result<ActionResult>.Success(new ActionResult(true, $"{source.Name} shoves {target.Name} back ({sourceRoll.Value.Total} vs {targetBest})."));
        }
    }
}
