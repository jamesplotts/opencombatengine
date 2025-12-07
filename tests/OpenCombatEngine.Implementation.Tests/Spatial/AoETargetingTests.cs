using System;
using System.Linq;
using FluentAssertions;
using Xunit;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Spatial;
using OpenCombatEngine.Implementation.Spatial.Shapes;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Items;

namespace OpenCombatEngine.Implementation.Tests.Spatial
{
    public class AoETargetingTests
    {
        private readonly StandardGridManager _grid;
        private readonly StandardCreature _caster;

        public AoETargetingTests()
        {
            _grid = new StandardGridManager();
            _caster = CreateCreature("Caster");
        }

        private StandardCreature CreateCreature(string name)
        {
            return new StandardCreature(
                Guid.NewGuid().ToString(),
                name,
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );
        }

        [Fact]
        public void Sphere_Should_Hit_Creatures_Within_Radius()
        {
            // Center (10, 10), Radius 2 (10ft/2 units)
            // Should hit (10,12), (12,10), (8,10).
            // Should miss (10,13).
            var c1 = CreateCreature("Inside1");
            var c2 = CreateCreature("Inside2");
            var c3 = CreateCreature("Outside");

            _grid.PlaceCreature(c1, new Position(10, 12));
            _grid.PlaceCreature(c2, new Position(12, 10));
            _grid.PlaceCreature(c3, new Position(10, 13));

            var shape = new SphereShape(2);
            var center = new Position(10, 10);

            var hits = _grid.GetCreaturesInShape(center, shape, null).ToList();

            hits.Should().Contain(c1);
            hits.Should().Contain(c2);
            hits.Should().NotContain(c3);
        }

        [Fact]
        public void Cube_Should_Hit_Creatures_Within_Bounds()
        {
            // Center (10, 10), Size 3 (3x3 units, Radius 1)
            // Extent: [9,11]
            var c1 = CreateCreature("Inside");
            var c2 = CreateCreature("Outside");

            _grid.PlaceCreature(c1, new Position(11, 11)); // Inside
            _grid.PlaceCreature(c2, new Position(12, 12)); // Outside

            var shape = new CubeShape(3);
            var center = new Position(10, 10);

            var hits = _grid.GetCreaturesInShape(center, shape, null).ToList();

            hits.Should().Contain(c1);
            hits.Should().NotContain(c2);
        }

        [Fact]
        public void Line_Should_Hit_Creatures_Along_Vector()
        {
            // Origin (10, 10), Target (20, 10) -> Horizontal Line East
            // Length 10, Width 1.
            var c1 = CreateCreature("Inside");
            var c2 = CreateCreature("Outside");

            _grid.PlaceCreature(c1, new Position(15, 10)); // On line
            _grid.PlaceCreature(c2, new Position(15, 11)); // 1 unit away (Dist 1). Width 1 -> Radius 0.5. DistSq 1 > 0.25. Outside.

            var shape = new LineShape(10, 1);
            var origin = new Position(10, 10);
            var target = new Position(20, 10);

            var hits = _grid.GetCreaturesInShape(origin, shape, target).ToList();

            hits.Should().Contain(c1);
            hits.Should().NotContain(c2);
        }

        [Fact]
        public void Cone_Should_Hit_Creatures_In_90_Degree_Arc()
        {
            // Origin (10, 10), Target (10, 20) -> North
            // Length 5.
            var c1 = CreateCreature("InsideCenter");
            var c2 = CreateCreature("InsideDiag");
            var c3 = CreateCreature("OutsideSide");
            var c4 = CreateCreature("OutsideBack");

            _grid.PlaceCreature(c1, new Position(10, 12)); // Straight N
            _grid.PlaceCreature(c2, new Position(11, 12)); // NNE (approx 26 deg). Should be inside 45 deg half-angle.
            _grid.PlaceCreature(c3, new Position(15, 12)); // Inside cone cone logic? 
            // Pos (15, 12). d=(5, 2). angle atan(5/2) = 68 deg. > 45. Outside.
            _grid.PlaceCreature(c4, new Position(10, 8)); // South. Outside.

            var shape = new ConeShape(5);
            var origin = new Position(10, 10);
            var target = new Position(10, 20);

            var hits = _grid.GetCreaturesInShape(origin, shape, target).ToList();

            hits.Should().Contain(c1);
            hits.Should().Contain(c2);
            hits.Should().NotContain(c3);
            hits.Should().NotContain(c4);
        }
    }
}
