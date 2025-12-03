using System.Collections.Generic;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Core.Interfaces.Spatial
{
    public interface IGridManager
    {
        Result<bool> PlaceCreature(ICreature creature, Position position);
        Result<bool> MoveCreature(ICreature creature, Position newPosition);
        Position? GetPosition(ICreature creature);
        ICreature? GetCreatureAt(Position position);
        int GetDistance(ICreature a, ICreature b);
        int GetDistance(Position a, Position b);

        void AddObstacle(Position position);
        void RemoveObstacle(Position position);
        bool IsObstructed(Position position);
        bool HasLineOfSight(Position source, Position target);
        IEnumerable<ICreature> GetCreaturesWithin(Position center, int radius);
        IEnumerable<ICreature> GetCreaturesInShape(Position origin, IShape shape, Position? direction = null);
        IEnumerable<ICreature> GetAllCreatures();

        void AddDifficultTerrain(Position position);
        void RemoveDifficultTerrain(Position position);
        bool IsDifficultTerrain(Position position);
        int GetPathCost(Position start, Position destination);
        bool IsFlanked(ICreature target, ICreature attacker);
        
        /// <summary>
        /// Gets the reach of the creature in feet (usually 5 or 10).
        /// </summary>
        int GetReach(ICreature creature);

        /// <summary>
        /// Gets the path of positions from start to destination.
        /// </summary>
        IEnumerable<Position> GetPath(Position start, Position destination);
    }
}
