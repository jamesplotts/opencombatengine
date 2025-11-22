using System;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Enums; // Added this using statement for ConditionType

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardMovement : IMovement
    {
        private readonly ICombatStats _stats;
        private readonly IConditionManager _conditions;

        public int Speed => _stats.Speed;
        public int MovementRemaining { get; private set; }

        public StandardMovement(ICombatStats stats, IConditionManager conditions)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
            MovementRemaining = Speed;
        }

        public void Move(int distance)
        {
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance), "Distance cannot be negative.");
            
            if (_conditions.HasCondition(ConditionType.Grappled))
            {
                throw new InvalidOperationException("Cannot move while Grappled.");
            }

            if (MovementRemaining < distance)
            {
                throw new InvalidOperationException($"Not enough movement remaining. Current: {MovementRemaining}, Requested: {distance}");
            }

            MovementRemaining -= distance;
        }

        public void ResetTurn()
        {
            if (_conditions.HasCondition(ConditionType.Grappled))
            {
                MovementRemaining = 0;
            }
            else
            {
                // Reset to current speed (which might have changed due to buffs/debuffs affecting stats)
                MovementRemaining = Speed;
            }
        }
    }
}
