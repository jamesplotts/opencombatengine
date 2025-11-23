using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Spatial;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class FlankingTests
    {
        private readonly StandardGridManager _grid;
        private readonly ICreature _attacker;
        private readonly ICreature _target;
        private readonly ICreature _ally;

        public FlankingTests()
        {
            _grid = new StandardGridManager();
            _attacker = Substitute.For<ICreature>();
            _target = Substitute.For<ICreature>();
            _ally = Substitute.For<ICreature>();

            _attacker.Name.Returns("Attacker");
            _attacker.Team.Returns("Player");
            _attacker.Id.Returns(System.Guid.NewGuid());

            _target.Name.Returns("Target");
            _target.Team.Returns("Monster");
            _target.Id.Returns(System.Guid.NewGuid());

            _ally.Name.Returns("Ally");
            _ally.Team.Returns("Player");
            _ally.Id.Returns(System.Guid.NewGuid());

            _grid.PlaceCreature(_attacker, new Position(0, 0, 0));
            _grid.PlaceCreature(_target, new Position(1, 0, 0));

            // Configure Action Economy
            _attacker.ActionEconomy.HasAction.Returns(true);
        }

        [Fact]
        public void IsFlanked_Should_Return_True_When_Ally_Is_Opposite()
        {
            // Attacker (0,0), Target (1,0). Opposite is (2,0).
            _grid.PlaceCreature(_ally, new Position(2, 0, 0));

            _grid.IsFlanked(_target, _attacker).Should().BeTrue();
        }

        [Fact]
        public void IsFlanked_Should_Return_False_When_Ally_Is_Not_Opposite()
        {
            // Attacker (0,0), Target (1,0). Ally at (1,1) (Adjacent but not opposite).
            _grid.PlaceCreature(_ally, new Position(1, 1, 0));

            _grid.IsFlanked(_target, _attacker).Should().BeFalse();
        }

        [Fact]
        public void IsFlanked_Should_Return_False_When_No_Ally_Present()
        {
            _grid.IsFlanked(_target, _attacker).Should().BeFalse();
        }

        [Fact]
        public void IsFlanked_Should_Return_False_When_Creature_At_Opposite_Is_Enemy()
        {
            // Enemy at opposite position
            var enemy = Substitute.For<ICreature>();
            enemy.Team.Returns("Monster");
            enemy.Id.Returns(System.Guid.NewGuid());
            
            _grid.PlaceCreature(enemy, new Position(2, 0, 0));

            _grid.IsFlanked(_target, _attacker).Should().BeFalse();
        }

        [Fact]
        public void AttackAction_Should_Roll_With_Advantage_When_Flanking()
        {
            // Arrange
            _grid.PlaceCreature(_ally, new Position(2, 0, 0)); // Flanking position

            var diceRoller = Substitute.For<IDiceRoller>();
            diceRoller.RollWithAdvantage(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<DiceRollResult>.Success(new DiceRollResult(20, "1d20", new System.Collections.Generic.List<int>{20}, 0, RollType.Advantage)));
            diceRoller.Roll(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<DiceRollResult>.Success(new DiceRollResult(10, "1d6", new System.Collections.Generic.List<int>{10}, 0, RollType.Normal))); // Damage

            var action = new AttackAction("Attack", "Desc", 5, "1d6", DamageType.Slashing, 2, diceRoller);
            
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _attacker,
                new OpenCombatEngine.Core.Models.Actions.CreatureTarget(_target),
                _grid
            );

            _target.ResolveAttack(Arg.Any<OpenCombatEngine.Core.Models.Combat.AttackResult>())
                .Returns(new OpenCombatEngine.Core.Models.Combat.AttackOutcome(true, 10, "Hit"));

            // Act
            var result = action.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue($"Execution failed: {result.Error}");
            diceRoller.Received(1).RollWithAdvantage(Arg.Any<string>());
        }
    }
}
