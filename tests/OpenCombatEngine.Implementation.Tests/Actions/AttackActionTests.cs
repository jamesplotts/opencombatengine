using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Actions
{
    public class AttackActionTests
    {
        private readonly IDiceRoller _diceRoller;
        private readonly ICreature _source;
        private readonly ICreature _target;

        public AttackActionTests()
        {
            _diceRoller = Substitute.For<IDiceRoller>();
            
            // Setup Source
            _source = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Attacker",
                new StandardAbilityScores(),
                new StandardHitPoints(10),
                new StandardCombatStats()
            );

            // Setup Target with AC 15, HP 20
            _target = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Defender",
                new StandardAbilityScores(),
                new StandardHitPoints(20),
                new StandardCombatStats(armorClass: 15)
            );
        }

        [Fact]
        public void Execute_Should_Hit_When_Roll_Meets_AC()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", attackBonus: 5, damageDice: "1d8", DamageType.Slashing, 0, _diceRoller);
            
            // Mock Attack Roll: 10 + 5 = 15 (Hits AC 15)
            _diceRoller.Roll("1d20+5").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(15, "1d20+5", new List<int> { 10 }, 5, RollType.Normal)));

            // Mock Damage Roll: 6
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            // Act
            // Act
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target)
            );
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Success.Should().BeTrue();
            result.Value.DamageDealt.Should().Be(6);
            result.Value.Message.Should().Contain("Hit");
            
            _target.HitPoints.Current.Should().Be(14); // 20 - 6
        }

        [Fact]
        public void Execute_Should_Miss_When_Roll_Below_AC()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", attackBonus: 5, damageDice: "1d8", DamageType.Slashing, 0, _diceRoller);
            
            // Mock Attack Roll: 9 + 5 = 14 (Misses AC 15)
            _diceRoller.Roll("1d20+5").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(14, "1d20+5", new List<int> { 9 }, 5, RollType.Normal)));

            // Mock Damage Roll (even on miss, it's rolled now)
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(
                new DiceRollResult(6, "1d8", new List<int> { 6 }, 0, RollType.Normal)));

            // Act
            // Act
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target)
            );
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue(); // The *execution* succeeded (didn't crash)
            result.Value.Success.Should().BeFalse(); // The *attack* missed
            result.Value.DamageDealt.Should().Be(0);
            result.Value.Message.Should().Contain("Missed");
            
            _target.HitPoints.Current.Should().Be(20); // No damage
        }

        [Fact]
        public void Execute_Should_Fail_If_Dice_Roll_Fails()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", attackBonus: 5, damageDice: "1d8", DamageType.Slashing, 0, _diceRoller);
            
            _diceRoller.Roll("1d20+5").Returns(Result<DiceRollResult>.Failure("Dice error"));

            // Act
            // Act
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target)
            );
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Dice error");
        }
        [Fact]
        public void Execute_Should_Consume_Action_When_Successful()
        {
            // Arrange
            var economy = Substitute.For<IActionEconomy>();
            economy.HasAction.Returns(true);
            
            var source = Substitute.For<ICreature>();
            source.ActionEconomy.Returns(economy);
            source.CombatStats.Returns(new StandardCombatStats()); // Needed for hit check? No, source stats not used for hit, only target AC.
            // Wait, AttackAction uses _attackBonus from constructor, doesn't look at source stats for bonus yet.

            var action = new AttackAction("Sword", "Slash", 5, "1d8", DamageType.Slashing, 0, _diceRoller);

            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(15, "1d20", new List<int> { 15 }, 0, RollType.Normal)));

            // Act
            // Act
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target)
            );
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            economy.Received(1).UseAction();
        }

        [Fact]
        public void Execute_Should_Fail_When_Action_Already_Used()
        {
            // Arrange
            var economy = Substitute.For<IActionEconomy>();
            economy.HasAction.Returns(false);
            
            var source = Substitute.For<ICreature>();
            source.ActionEconomy.Returns(economy);

            var action = new AttackAction("Sword", "Slash", 5, "1d8", DamageType.Slashing, 0, _diceRoller);

            // Act
            // Act
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target)
            );
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Resource already used");
            _diceRoller.DidNotReceive().Roll(Arg.Any<string>());
        }
        [Fact]
        public void Execute_Should_Roll_With_Disadvantage_When_Prone()
        {
            // Arrange
            var economy = Substitute.For<IActionEconomy>();
            economy.HasAction.Returns(true);
            
            var conditions = Substitute.For<IConditionManager>();
            conditions.HasCondition(ConditionType.Prone).Returns(true);

            var source = Substitute.For<ICreature>();
            source.ActionEconomy.Returns(economy);
            source.Conditions.Returns(conditions);

            var action = new AttackAction("Sword", "Slash", 5, "1d8", DamageType.Slashing, 0, _diceRoller);

            _diceRoller.RollWithDisadvantage(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(10, "1d20", new List<int> { 10, 5 }, 0, RollType.Disadvantage)));
            _diceRoller.Roll("1d8").Returns(Result<DiceRollResult>.Success(new DiceRollResult(5, "1d8", new List<int> { 5 }, 0, RollType.Normal)));

            // Act
            // Act
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target)
            );
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _diceRoller.Received(1).RollWithDisadvantage(Arg.Any<string>());
            _diceRoller.DidNotReceive().Roll(Arg.Is<string>(s => s.Contains("1d20"))); // Should not roll normal attack
            _diceRoller.Received(1).Roll(Arg.Is<string>(s => s.Contains("1d8"))); // Should roll damage
        }
        [Fact]
        public void Execute_Should_Fail_When_Target_Out_Of_Range()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", 5, "1d8", DamageType.Slashing, 0, _diceRoller, range: 5);
            
            var grid = Substitute.For<OpenCombatEngine.Core.Interfaces.Spatial.IGridManager>();
            var sourcePos = new OpenCombatEngine.Core.Models.Spatial.Position(0, 0, 0);
            var targetPos = new OpenCombatEngine.Core.Models.Spatial.Position(10, 0, 0); // 10ft away

            grid.GetPosition(_source).Returns(sourcePos);
            grid.GetPosition(_target).Returns(targetPos);
            grid.GetDistance(sourcePos, targetPos).Returns(10);

            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target),
                grid
            );

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("out of range");
        }

        [Fact]
        public void Execute_Should_Succeed_When_Target_In_Range()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", 5, "1d8", DamageType.Slashing, 0, _diceRoller, range: 10);
            
            var grid = Substitute.For<OpenCombatEngine.Core.Interfaces.Spatial.IGridManager>();
            var sourcePos = new OpenCombatEngine.Core.Models.Spatial.Position(0, 0, 0);
            var targetPos = new OpenCombatEngine.Core.Models.Spatial.Position(10, 0, 0); // 10ft away

            grid.GetPosition(_source).Returns(sourcePos);
            grid.GetPosition(_target).Returns(targetPos);
            grid.GetDistance(sourcePos, targetPos).Returns(10);
            grid.HasLineOfSight(sourcePos, targetPos).Returns(true);

            // Mock successful roll
            _diceRoller.Roll(Arg.Any<string>()).Returns(Result<DiceRollResult>.Success(new DiceRollResult(15, "1d20", new List<int> { 15 }, 0, RollType.Normal)));

            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target),
                grid
            );

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Execute_Should_Fail_When_LOS_Blocked()
        {
            // Arrange
            var action = new AttackAction("Sword", "Slash", 5, "1d8", DamageType.Slashing, 0, _diceRoller, range: 10);
            
            var grid = Substitute.For<OpenCombatEngine.Core.Interfaces.Spatial.IGridManager>();
            var sourcePos = new OpenCombatEngine.Core.Models.Spatial.Position(0, 0, 0);
            var targetPos = new OpenCombatEngine.Core.Models.Spatial.Position(10, 0, 0); // 10ft away

            grid.GetPosition(_source).Returns(sourcePos);
            grid.GetPosition(_target).Returns(targetPos);
            grid.GetDistance(sourcePos, targetPos).Returns(10);
            grid.HasLineOfSight(sourcePos, targetPos).Returns(false); // Blocked

            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _source, 
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target),
                grid
            );

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("No line of sight");
        }
    }
}
