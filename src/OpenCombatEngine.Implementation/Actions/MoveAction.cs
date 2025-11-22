using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Actions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Actions;
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

        public Result<ActionResult> Execute(ICreature source, ICreature target)
        {
            ArgumentNullException.ThrowIfNull(source);
            // Target is irrelevant for movement, but required by interface. Can be null or ignored.

            if (source.Movement == null)
            {
                return Result<ActionResult>.Failure("Creature has no movement capability.");
            }

            if (source.Movement.MovementRemaining < _distance)
            {
                return Result<ActionResult>.Failure($"Not enough movement. Required: {_distance}, Remaining: {source.Movement.MovementRemaining}");
            }

            source.Movement.Move(_distance);
            return Result<ActionResult>.Success(new ActionResult(true, $"Moved {_distance} feet."));
        }
    }
}
