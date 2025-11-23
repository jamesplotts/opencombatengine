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

        private readonly HashSet<Position> _obstacles = new();

        public void AddObstacle(Position position)
        {
            _obstacles.Add(position);
        }

        public void RemoveObstacle(Position position)
        {
            _obstacles.Remove(position);
        }

        public bool IsObstructed(Position position)
        {
            return _obstacles.Contains(position);
        }

        public bool HasLineOfSight(Position source, Position target)
        {
            // 3D Bresenham's Line Algorithm
            // We need to check every point along the line from source to target.
            // If any point is an obstacle, return false.
            // Source and Target themselves are usually excluded from the "blocking" check 
            // (i.e., if target is IN an obstacle, you can still see them? Maybe not. 
            // But if source is IN an obstacle, they can't see out? 
            // For now, let's check strictly between source and target).

            int x1 = source.X;
            int y1 = source.Y;
            int z1 = source.Z;
            int x2 = target.X;
            int y2 = target.Y;
            int z2 = target.Z;

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int dz = Math.Abs(z2 - z1);

            int xs = (x2 > x1) ? 1 : -1;
            int ys = (y2 > y1) ? 1 : -1;
            int zs = (z2 > z1) ? 1 : -1;

            // Driving axis is the one with max difference
            if (dx >= dy && dx >= dz)
            {
                int p1 = 2 * dy - dx;
                int p2 = 2 * dz - dx;
                while (x1 != x2)
                {
                    x1 += xs;
                    if (p1 >= 0)
                    {
                        y1 += ys;
                        p1 -= 2 * dx;
                    }
                    if (p2 >= 0)
                    {
                        z1 += zs;
                        p2 -= 2 * dx;
                    }
                    p1 += 2 * dy;
                    p2 += 2 * dz;

                    // Check if this point is an obstacle
                    // Don't check the target point itself (we want to see the target even if they are in a "blocked" cell, 
                    // though typically creatures aren't in obstacles. But let's be safe).
                    if (x1 == x2 && y1 == y2 && z1 == z2) break; 

                    if (IsObstructed(new Position(x1, y1, z1))) return false;
                }
            }
            else if (dy >= dx && dy >= dz)
            {
                int p1 = 2 * dx - dy;
                int p2 = 2 * dz - dy;
                while (y1 != y2)
                {
                    y1 += ys;
                    if (p1 >= 0)
                    {
                        x1 += xs;
                        p1 -= 2 * dy;
                    }
                    if (p2 >= 0)
                    {
                        z1 += zs;
                        p2 -= 2 * dy;
                    }
                    p1 += 2 * dx;
                    p2 += 2 * dz;

                    if (x1 == x2 && y1 == y2 && z1 == z2) break;
                    if (IsObstructed(new Position(x1, y1, z1))) return false;
                }
            }
            else
            {
                int p1 = 2 * dy - dz;
                int p2 = 2 * dx - dz;
                while (z1 != z2)
                {
                    z1 += zs;
                    if (p1 >= 0)
                    {
                        y1 += ys;
                        p1 -= 2 * dz;
                    }
                    if (p2 >= 0)
                    {
                        x1 += xs;
                        p2 -= 2 * dz;
                    }
                    p1 += 2 * dy;
                    p2 += 2 * dx;

                    if (x1 == x2 && y1 == y2 && z1 == z2) break;
                    if (IsObstructed(new Position(x1, y1, z1))) return false;
                }
            }

            return true;
        }

        public IEnumerable<ICreature> GetCreaturesInShape(Position origin, IShape shape, Position? direction = null)
        {
            ArgumentNullException.ThrowIfNull(shape);

            foreach (var kvp in _creaturePositions)
            {
                var pos = kvp.Value;
                if (shape.Contains(origin, pos, direction))
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

        private readonly System.Collections.Generic.HashSet<Position> _difficultTerrain = new();

        public void AddDifficultTerrain(Position position)
        {
            _difficultTerrain.Add(position);
        }

        public void RemoveDifficultTerrain(Position position)
        {
            _difficultTerrain.Remove(position);
        }

        public bool IsDifficultTerrain(Position position)
        {
            return _difficultTerrain.Contains(position);
        }

        public int GetPathCost(Position start, Position destination)
        {
            // Simplified path cost: Straight line (Bresenham)
            // Cost = Sum of costs of entering each cell.
            // Start cell cost is NOT included (you are already there).
            
            var points = GetLinePoints(start, destination);
            int totalCost = 0;

            // Skip the first point (start)
            foreach (var point in System.Linq.Enumerable.Skip(points, 1))
            {
                int stepCost = IsDifficultTerrain(point) ? 10 : 5;
                totalCost += stepCost;
            }

            return totalCost;
        }

        // Helper to expose line points (refactored from HasLineOfSight or duplicated for now)
        private static System.Collections.Generic.IEnumerable<Position> GetLinePoints(Position p1, Position p2)
        {
            int x1 = p1.X, y1 = p1.Y, z1 = p1.Z;
            int x2 = p2.X, y2 = p2.Y, z2 = p2.Z;

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int dz = Math.Abs(z2 - z1);

            int xs = x1 < x2 ? 1 : -1;
            int ys = y1 < y2 ? 1 : -1;
            int zs = z1 < z2 ? 1 : -1;

            // 3D Bresenham is complex.
            // Let's use a simpler approach: Max(dx, dy, dz) steps.
            // This is equivalent to Chebyshev distance steps.
            
            int steps = Math.Max(dx, Math.Max(dy, dz));
            
            // Yield start
            yield return p1;

            if (steps == 0) yield break;

            for (int i = 1; i <= steps; i++)
            {
                // Interpolate
                // Note: This is a rough approximation for grid movement.
                // Ideally we'd use a proper 3D line algorithm.
                // But for "Chebyshev" movement (diagonals are free), we just need to visit cells.
                
                int x = x1 + (dx == 0 ? 0 : (int)Math.Round((double)dx * i / steps) * xs);
                // Wait, simple interpolation:
                // x = x1 + i * (x2 - x1) / steps ?? No, integer division issues.
                
                // Let's stick to the logic used in HasLineOfSight if possible, or a standard one.
                // Actually, let's just use the same logic as HasLineOfSight but yield points.
                
                // Re-implementing Bresenham cleanly:
                // For 3D, we can drive by the major axis.
                
                // But wait, if we use Chebyshev distance (5-5-5), we just take 'steps' moves.
                // Each move brings us closer.
                
                int cx = x1 + (int)Math.Round((double)(x2 - x1) * i / steps);
                int cy = y1 + (int)Math.Round((double)(y2 - y1) * i / steps);
                int cz = z1 + (int)Math.Round((double)(z2 - z1) * i / steps);
                
                yield return new Position(cx, cy, cz);
            }
        }
    }
}
