using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions.Contexts;
using OpenCombatEngine.Implementation.Actions.Spells;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spatial;

namespace OpenCombatEngine.Implementation.Tests.Actions.Spells
{
    public class FireballTests
    {
        private class FakeDiceRoller : IDiceRoller
        {
            public int NextRoll { get; set; } = 10;
            public int DamageRoll { get; set; } = 28;

            public Result<DiceRollResult> Roll(string diceNotation)
            {
                // If notation includes "8d6", return DamageRoll
                if (diceNotation.Contains("8d6"))
                {
                    return Result<DiceRollResult>.Success(new DiceRollResult(DamageRoll, diceNotation, new List<int> { DamageRoll }, 0, RollType.Normal));
                }
                
                // For Saves (e.g. "1d20+5")
                // Parse mod
                int mod = 0;
                if (diceNotation.Contains("+"))
                {
                    var parts = diceNotation.Split('+');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int m))
                    {
                        mod = m;
                    }
                }
                
                int total = NextRoll + mod;
                
                // Return NextRoll as the die roll, but Total adjusted by mod (if built-in logic was used).
                // Actually StandardCheckManager relies on DiceRoller returning the Total.
                
                return Result<DiceRollResult>.Success(new DiceRollResult(total, diceNotation, new List<int> { NextRoll }, mod, RollType.Normal));
            }
            
            public Result<DiceRollResult> RollWithAdvantage(string diceNotation) => Roll(diceNotation);
            public Result<DiceRollResult> RollWithDisadvantage(string diceNotation) => Roll(diceNotation);
            public bool IsValidNotation(string diceNotation) => true;
            public int? Seed { get; set; } = 0;
        }

        private readonly StandardGridManager _grid;
        private readonly FakeDiceRoller _dice;

        public FireballTests()
        {
            _grid = new StandardGridManager();
            _dice = new FakeDiceRoller();
        }

        private StandardCreature CreateCreature(string name, int dexScore)
        {
            // Dex Score -> Modifier: (Score - 10) / 2
            // 10 -> 0
            // 20 -> +5
            return new StandardCreature(
                Guid.NewGuid().ToString(),
                name,
                new StandardAbilityScores(10, dexScore, 10, 10, 10, 10),
                new StandardHitPoints(100), // High HP to survive
                new StandardInventory(),
                new StandardTurnManager(_dice) // Uses fake dice for check manager
            );
        }

        [Fact]
        public void Fireball_Should_Damage_Creatures_Based_On_Saves()
        {
            // Arrange
            var caster = CreateCreature("Caster", 10);
            var victimFail = CreateCreature("VictimFail", 10); // Dex 10 (+0).
            var victimSave = CreateCreature("VictimSave", 20); // Dex 20 (+5).

            _grid.PlaceCreature(caster, new Position(0, 0));
            _grid.PlaceCreature(victimFail, new Position(10, 10)); // Fail target
            _grid.PlaceCreature(victimSave, new Position(10, 12)); // Save target
            
            _dice.NextRoll = 10;
            _dice.DamageRoll = 28;

            var action = new FireballAction("Fireball", "Kaboom", 15, _dice);
            var context = new StandardActionContext(
                caster, 
                new PositionTarget(new Position(10, 10)), 
                _grid
            );

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            
            // VictimFail: 10 + 0 = 10 < 15. Fail. Take 28.
            victimFail.HitPoints.Current.Should().Be(72); // 100 - 28

            // VictimSave: 10 + 5 = 15 >= 15. Save. Take 14.
            victimSave.HitPoints.Current.Should().Be(86); // 100 - 14
        }
        
        [Fact]
        public void Fireball_Should_Respect_Shape_Radius()
        {
            // Arrange
            var caster = CreateCreature("Caster", 10);
            var inside = CreateCreature("Inside", 10);
            var outside = CreateCreature("Outside", 10);

            _grid.PlaceCreature(caster, new Position(0, 0));
            _grid.PlaceCreature(inside, new Position(10, 12)); // Dist 2. Inside.
            _grid.PlaceCreature(outside, new Position(10, 15)); // Dist 5. Outside.

            var action = new FireballAction("Fireball", "Kaboom", 15, _dice);
            var context = new StandardActionContext(
                caster, 
                new PositionTarget(new Position(10, 10)), 
                _grid
            );

            // Act
            action.Execute(context);

            // Assert
            inside.HitPoints.Current.Should().BeLessThan(100);
            outside.HitPoints.Current.Should().Be(100);
        }
    }
}
