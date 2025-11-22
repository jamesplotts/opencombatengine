using System;
using OpenCombatEngine.Core.Interfaces.Creatures;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardMovement : IMovement
    {
        private readonly ICombatStats _combatStats;

        public int Speed => _combatStats.Speed;
        public int MovementRemaining { get; private set; }

        public StandardMovement(ICombatStats combatStats)
        {
            _combatStats = combatStats ?? throw new ArgumentNullException(nameof(combatStats));
            MovementRemaining = Speed;
        }

        public void Move(int distance)
        {
            if (distance < 0) throw new ArgumentException("Distance cannot be negative", nameof(distance));
            
            if (distance > MovementRemaining)
            {
                // In a real engine, this might return a Result or throw.
                // For now, we'll just cap it or throw. 
                // Let's cap it to remaining to be safe, or throw to indicate illegal move?
                // The interface implies "Move", so let's throw if illegal.
                throw new InvalidOperationException($"Cannot move {distance}ft. Only {MovementRemaining}ft remaining.");
            }

            MovementRemaining -= distance;
        }

        public void ResetTurn()
        {
            // Reset to current speed (which might have changed due to buffs/debuffs affecting stats)
            MovementRemaining = Speed;
        }
    }
}
