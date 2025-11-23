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
        IEnumerable<ICreature> GetAllCreatures();
    }
}
