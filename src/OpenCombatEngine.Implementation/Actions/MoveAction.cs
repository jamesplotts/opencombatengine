using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Actions
{
    public class MoveAction : IAction
    {
        public string Name => "Move";
        public string Description => $"Move {_distance} feet.";
        public ActionType Type => ActionType.Movement;

        private readonly int _distance;

        public MoveAction(int distance)
        {
            if (distance <= 0) throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be positive.");
            _distance = distance;
        }

        public Result<ActionResult> Execute(IActionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var source = context.Source;

            if (source.Movement == null)
            {
                return Result<ActionResult>.Failure("Creature has no movement capability.");
            }

            // If Grid is present, validate position and distance
            if (context.Grid != null)
            {
                if (context.Target is not PositionTarget positionTarget)
                {
                    return Result<ActionResult>.Failure("Target must be a position for grid movement.");
                }

                var currentPos = context.Grid.GetPosition(source);
                if (currentPos == null)
                {
                    return Result<ActionResult>.Failure("Creature is not on the grid.");
                }

                var targetPos = positionTarget.Position;
                var distance = context.Grid.GetDistance(currentPos.Value, targetPos);

                if (distance > _distance)
                {
                    return Result<ActionResult>.Failure($"Distance {distance} exceeds action range {_distance}.");
                }

                if (source.Movement.MovementRemaining < distance)
                {
                    return Result<ActionResult>.Failure($"Not enough movement. Required: {distance}, Remaining: {source.Movement.MovementRemaining}");
                }

                var moveResult = context.Grid.MoveCreature(source, targetPos);
                if (!moveResult.IsSuccess)
                {
                    return Result<ActionResult>.Failure(moveResult.Error);
                }

                source.Movement.Move(distance);
                return Result<ActionResult>.Success(new ActionResult(true, $"Moved {distance} feet to {targetPos}."));
            }
            else
            {
                // Fallback for non-grid movement (abstract)
                // Just deduct distance? But we don't know "how far" unless target is abstract distance?
                // For now, assume full distance used if no grid? Or just succeed?
                // Let's assume non-grid movement just deducts the max distance of the action?
                // Or maybe we shouldn't support non-grid movement in MoveAction anymore if it takes a distance?
                // MoveAction(30) usually means "Move up to 30".
                // Without grid, we can't validate "where".
                // So we just deduct 0? Or assume success?
                // Let's just deduct _distance for now to simulate "I moved 30ft".
                
                if (source.Movement.MovementRemaining < _distance)
                {
                    return Result<ActionResult>.Failure($"Not enough movement. Required: {_distance}, Remaining: {source.Movement.MovementRemaining}");
                }
                source.Movement.Move(_distance);
                return Result<ActionResult>.Success(new ActionResult(true, $"Moved {_distance} feet (Abstract)."));
            }
        }
    }
}
