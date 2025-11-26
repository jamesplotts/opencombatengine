using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Features;
using OpenCombatEngine.Implementation.Items;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class SneakAttackTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly StandardCreature _rogue;
        private readonly SneakAttackFeature _sneakAttack;

        public SneakAttackTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            _rogue = new StandardCreature(
                System.Guid.NewGuid().ToString(),
                "Rogue",
                new StandardAbilityScores(),
                new StandardHitPoints(20),
                new StandardInventory(),
                new StandardTurnManager(new StandardDiceRoller())
            );
            _rogue.Team = "Neutral";

            _sneakAttack = new SneakAttackFeature(_diceRoller, 1); // 1d6
            _rogue.AddFeature(_sneakAttack);

            // Equip Finesse Weapon (Dagger)
            var dagger = new Weapon("Dagger", "1d4", DamageType.Piercing, new[] { WeaponProperty.Finesse, WeaponProperty.Light });
            _rogue.Equipment.EquipMainHand(dagger);
        }

        [Fact]
        public void Should_Add_Damage_When_Advantage_Present()
        {
            // Arrange
            var dummyTarget = Substitute.For<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>();
            var attack = new AttackResult(
                _rogue, 
                dummyTarget, 
                20, 
                false, 
                true, // HasAdvantage
                false, 
                new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) }
            );

            _diceRoller.Roll("1d6").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d6", new List<int> { 6 }, 0, RollType.Normal)));

            // Act
            _rogue.ModifyOutgoingAttack(attack);

            // Assert
            attack.Damage.Should().HaveCount(2);
            attack.Damage.Last().Amount.Should().Be(6);
            attack.Damage.Last().Type.Should().Be(DamageType.Piercing);
        }

        [Fact]
        public void Should_Not_Add_Damage_When_Disadvantage_Present()
        {
            // Arrange
            var dummyTarget = Substitute.For<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>();
            var attack = new AttackResult(
                _rogue, 
                dummyTarget, 
                20, 
                false, 
                true, // HasAdvantage (cancelled by Disadvantage in logic? No, logic checks HasDisadvantage explicitly)
                true, // HasDisadvantage
                new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) }
            );

            // Act
            _rogue.ModifyOutgoingAttack(attack);

            // Assert
            attack.Damage.Should().HaveCount(1);
        }

        [Fact]
        public void Should_Not_Add_Damage_Without_Advantage_Or_Ally()
        {
            // Arrange
            var dummyTarget = Substitute.For<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>();
            var attack = new AttackResult(
                _rogue, 
                dummyTarget, 
                20, 
                false, 
                false, // No Advantage
                false, 
                new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) }
            );

            // Act
            _rogue.ModifyOutgoingAttack(attack);

            // Assert
            attack.Damage.Should().HaveCount(1);
        }

        [Fact]
        public void Should_Add_Damage_When_Ally_Adjacent()
        {
            // Arrange
            _sneakAttack.IsAllyAdjacent = true; // Simulate ally

            var dummyTarget = Substitute.For<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>();
            var attack = new AttackResult(
                _rogue, 
                dummyTarget, 
                20, 
                false, 
                false, // No Advantage
                false, 
                new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) }
            );

            _diceRoller.Roll("1d6").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d6", new List<int> { 6 }, 0, RollType.Normal)));

            // Act
            _rogue.ModifyOutgoingAttack(attack);

            // Assert
            attack.Damage.Should().HaveCount(2);
        }

        [Fact]
        public void Should_Only_Apply_Once_Per_Turn()
        {
            // Arrange
            var dummyTarget = Substitute.For<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>();
            var attack1 = new AttackResult(_rogue, dummyTarget, 20, false, true, false, new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) });
            var attack2 = new AttackResult(_rogue, dummyTarget, 20, false, true, false, new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) });

            _diceRoller.Roll("1d6").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d6", new List<int> { 6 }, 0, RollType.Normal)));

            // Act
            _rogue.ModifyOutgoingAttack(attack1);
            _rogue.ModifyOutgoingAttack(attack2);

            // Assert
            attack1.Damage.Should().HaveCount(2);
            attack2.Damage.Should().HaveCount(1);
        }

        [Fact]
        public void Should_Reset_On_StartTurn()
        {
            // Arrange
            var dummyTarget = Substitute.For<OpenCombatEngine.Core.Interfaces.Creatures.ICreature>();
            var attack1 = new AttackResult(_rogue, dummyTarget, 20, false, true, false, new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) });
            var attack2 = new AttackResult(_rogue, dummyTarget, 20, false, true, false, new List<DamageRoll> { new DamageRoll(4, DamageType.Piercing) });

            _diceRoller.Roll("1d6").Returns(Result<DiceRollResult>.Success(new DiceRollResult(6, "1d6", new List<int> { 6 }, 0, RollType.Normal)));

            // Act
            _rogue.ModifyOutgoingAttack(attack1);
            _rogue.StartTurn(); // Reset
            _rogue.ModifyOutgoingAttack(attack2);

            // Assert
            attack1.Damage.Should().HaveCount(2);
            attack2.Damage.Should().HaveCount(2);
        }
    }
}
