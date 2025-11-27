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
        private int _movementUsed;

        public ICreature? Creature { get; set; }

        public int Speed
        {
            get
            {
                if (_conditions.HasCondition(ConditionType.Grappled) || 
                    _conditions.HasCondition(ConditionType.Restrained) ||
                    _conditions.HasCondition(ConditionType.Unconscious) ||
                    _conditions.HasCondition(ConditionType.Paralyzed) ||
                    _conditions.HasCondition(ConditionType.Petrified) ||
                    _conditions.HasCondition(ConditionType.Stunned))
                {
                    return 0;
                }

                var baseSpeed = _stats.Speed;
                
                // Apply encumbrance penalties
                if (Creature != null)
                {
                    var encumbrance = Creature.EncumbranceLevel;
                    if (encumbrance == OpenCombatEngine.Core.Enums.EncumbranceLevel.OverCapacity) return 5;
                    
                    if (encumbrance == OpenCombatEngine.Core.Enums.EncumbranceLevel.HeavilyEncumbered) baseSpeed -= 20;
                    else if (encumbrance == OpenCombatEngine.Core.Enums.EncumbranceLevel.Encumbered) baseSpeed -= 10;
                }

                return System.Math.Max(0, baseSpeed);
            }
        }

        public int MovementRemaining => System.Math.Max(0, Speed - _movementUsed);

        public StandardMovement(ICombatStats stats, IConditionManager conditions)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool IsInDifficultTerrain { get; set; }

        public void Move(int distance)
        {
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance), "Distance cannot be negative.");
            
            if (_conditions.HasCondition(ConditionType.Grappled))
            {
                throw new InvalidOperationException("Cannot move while Grappled.");
            }

            int cost = IsInDifficultTerrain ? distance * 2 : distance;

            if (MovementRemaining < cost)
            {
                throw new InvalidOperationException($"Not enough movement remaining. Current: {MovementRemaining}, Cost: {cost} (Distance: {distance}, Difficult Terrain: {IsInDifficultTerrain})");
            }

            _movementUsed += cost;
        }

        public void ResetTurn()
        {
            _movementUsed = 0;
        }
    }
}
