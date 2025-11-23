using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Spatial
{
    public class StandardGridManager : IGridManager
    {
        private readonly Dictionary<Guid, Position> _creaturePositions = new();
        private readonly Dictionary<Position, Guid> _positionCreatures = new();

        public Result<bool> PlaceCreature(ICreature creature, Position position)
        {
            if (creature == null) return Result<bool>.Failure("Creature cannot be null.");

            if (_creaturePositions.ContainsKey(creature.Id))
            {
                return Result<bool>.Failure("Creature is already placed on the grid.");
            }

            if (_positionCreatures.ContainsKey(position))
            {
                return Result<bool>.Failure("Position is already occupied.");
            }

            _creaturePositions[creature.Id] = position;
            _positionCreatures[position] = creature.Id;
            
            // We need to store the creature reference itself?
            // The interface methods like GetCreatureAt return ICreature.
            // So we need a way to look up ICreature by Id.
            // For now, let's store the ICreature in a separate dictionary or just assume we can't unless we store it.
            // I'll add a dictionary for ID -> ICreature.
            _creatures[creature.Id] = creature;

            return Result<bool>.Success(true);
        }

        private readonly Dictionary<Guid, ICreature> _creatures = new();

        public Result<bool> MoveCreature(ICreature creature, Position newPosition)
        {
            if (creature == null) return Result<bool>.Failure("Creature cannot be null.");
            if (!_creaturePositions.TryGetValue(creature.Id, out var oldPos)) return Result<bool>.Failure("Creature is not on the grid.");

            if (_positionCreatures.ContainsKey(newPosition))
            {
                return Result<bool>.Failure("Target position is occupied.");
            }

            _positionCreatures.Remove(oldPos);
            
            _creaturePositions[creature.Id] = newPosition;
            _positionCreatures[newPosition] = creature.Id;

            return Result<bool>.Success(true);
        }

        public Position? GetPosition(ICreature creature)
        {
            if (creature == null) return null;
            if (_creaturePositions.TryGetValue(creature.Id, out var pos)) return pos;
            return null;
        }

        public ICreature? GetCreatureAt(Position position)
        {
            if (_positionCreatures.TryGetValue(position, out var id))
            {
                return _creatures.TryGetValue(id, out var creature) ? creature : null;
            }
            return null;
        }

        public int GetDistance(ICreature a, ICreature b)
        {
            var posA = GetPosition(a);
            var posB = GetPosition(b);

            if (posA == null || posB == null) return int.MaxValue; // Or throw? MaxValue indicates "out of range" effectively.

            return GetDistance(posA.Value, posB.Value);
        }

        public int GetDistance(Position a, Position b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            int dz = Math.Abs(a.Z - b.Z);

            // Chebyshev distance: Max of diffs * 5
            return Math.Max(dx, Math.Max(dy, dz)) * 5;
        }

        public IEnumerable<ICreature> GetCreaturesWithin(Position center, int radius)
        {
            foreach (var kvp in _creaturePositions)
            {
                var pos = kvp.Value;
                if (GetDistance(center, pos) <= radius)
                {
                    if (_creatures.TryGetValue(kvp.Key, out var creature))
                    {
                        yield return creature;
                    }
                }
            }
        }

        public IEnumerable<ICreature> GetAllCreatures()
        {
            return _creatures.Values;
        }
    }
}
