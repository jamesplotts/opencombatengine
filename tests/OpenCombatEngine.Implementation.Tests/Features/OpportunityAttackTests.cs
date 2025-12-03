using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Models.Actions;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Actions;
using OpenCombatEngine.Implementation.Spatial;
using Xunit;
using System.Collections.Generic;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class OpportunityAttackTests
    {
        private readonly StandardGridManager _grid;
        private readonly ICreature _mover;
        private readonly ICreature _attacker;
        private readonly IDiceRoller _diceRoller;

        public OpportunityAttackTests()
        {
            _grid = new StandardGridManager();
            _mover = Substitute.For<ICreature>();
            _attacker = Substitute.For<ICreature>();
            _diceRoller = Substitute.For<IDiceRoller>();

            _mover.Name.Returns("Mover");
            _mover.Team.Returns("Player");
            _mover.Id.Returns(System.Guid.NewGuid());
            _mover.Movement.Returns(Substitute.For<IMovement>());
            _mover.Movement.MovementRemaining.Returns(30);
            _mover.HitPoints.Returns(Substitute.For<IHitPoints>());
            _mover.HitPoints.Current.Returns(10);

            _attacker.Name.Returns("Attacker");
            _attacker.Team.Returns("Monster");
            _attacker.Id.Returns(System.Guid.NewGuid());
            _attacker.ActionEconomy.Returns(Substitute.For<IActionEconomy>());
            _attacker.ActionEconomy.HasReaction.Returns(true);
            
            // Mock Equipment for Attacker
            var equipment = Substitute.For<OpenCombatEngine.Core.Interfaces.Items.IEquipmentManager>();
            equipment.MainHand.Returns((OpenCombatEngine.Core.Interfaces.Items.IWeapon)null);
            _attacker.Equipment.Returns(equipment);

            _grid.PlaceCreature(_mover, new Position(0, 0, 0));
            _grid.PlaceCreature(_attacker, new Position(1, 1, 0)); // Adjacent (diagonal)
        }

        [Fact]
        public void Moving_Out_Of_Reach_Should_Trigger_Opportunity_Attack()
        {
            // Arrange
            // Attacker is at (1,1). Reach is 5.
            // Mover is at (0,0). Distance is 5.
            // Mover moves to (-1, -1). Distance becomes 10 (Chebyshev) or more?
            // (-1,-1) to (1,1) -> dx=2, dy=2. Distance = 10.
            // So moving from (0,0) [dist 5] to (-1,-1) [dist 10] leaves reach.
            
            var moveAction = new MoveAction(30, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _mover,
                new PositionTarget(new Position(-1, -1, 0)),
                _grid
            );

            // Mock Dice Roll for Attack
            _diceRoller.Roll(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<DiceRollResult>.Success(new DiceRollResult(15, "1d20", new List<int>{15}, 0, RollType.Normal)));
            
            // Mock Mover resolving attack
            _mover.ResolveAttack(Arg.Any<OpenCombatEngine.Core.Models.Combat.AttackResult>())
                .Returns(new OpenCombatEngine.Core.Models.Combat.AttackOutcome(true, 5, "Hit"));

            // Act
            var result = moveAction.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // Attacker used reaction
            _attacker.ActionEconomy.Received(1).UseReaction();
            // Mover received attack
            _mover.Received(1).ResolveAttack(Arg.Any<OpenCombatEngine.Core.Models.Combat.AttackResult>());
        }

        [Fact]
        public void Moving_Within_Reach_Should_Not_Trigger_Opportunity_Attack()
        {
            // Arrange
            // Attacker at (1,1). Reach 5.
            // Mover at (0,0). Dist 5.
            // Mover moves to (0,1). Dist 5 (Adjacent).
            
            var moveAction = new MoveAction(30, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _mover,
                new PositionTarget(new Position(0, 1, 0)),
                _grid
            );

            // Act
            var result = moveAction.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _attacker.ActionEconomy.DidNotReceive().UseReaction();
            _mover.DidNotReceive().ResolveAttack(Arg.Any<OpenCombatEngine.Core.Models.Combat.AttackResult>());
        }

        [Fact]
        public void No_Reaction_Should_Prevent_Opportunity_Attack()
        {
            // Arrange
            _attacker.ActionEconomy.HasReaction.Returns(false);
            
            var moveAction = new MoveAction(30, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _mover,
                new PositionTarget(new Position(-1, -1, 0)), // Leaving reach
                _grid
            );

            // Act
            var result = moveAction.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _attacker.ActionEconomy.DidNotReceive().UseReaction();
            _mover.DidNotReceive().ResolveAttack(Arg.Any<OpenCombatEngine.Core.Models.Combat.AttackResult>());
        }
        
        [Fact]
        public void Reach_Weapon_Should_Extend_Threat_Range()
        {
            // Arrange
            // Attacker has Reach weapon (10ft).
            // Attacker at (2,2).
            // Mover at (0,0). Dist to (2,2) is Max(2,2)*5 = 10ft. In Reach.
            // Mover moves to (-1,-1). Dist to (2,2) is Max(3,3)*5 = 15ft. Out of Reach.
            
            _grid.MoveCreature(_attacker, new Position(2, 2, 0));
            
            // Mock Reach Weapon
            var weapon = Substitute.For<OpenCombatEngine.Core.Interfaces.Items.IWeapon>();
            weapon.Properties.Returns(new List<WeaponProperty> { WeaponProperty.Reach });
            weapon.DamageDice.Returns("1d10");
            weapon.DamageType.Returns(DamageType.Slashing);
            _attacker.Equipment.MainHand.Returns(weapon);

            var moveAction = new MoveAction(30, _diceRoller);
            var context = new OpenCombatEngine.Implementation.Actions.Contexts.StandardActionContext(
                _mover,
                new PositionTarget(new Position(-1, -1, 0)),
                _grid
            );
            
            // Mock Dice
             _diceRoller.Roll(Arg.Any<string>()).Returns(OpenCombatEngine.Core.Results.Result<DiceRollResult>.Success(new DiceRollResult(15, "1d20", new List<int>{15}, 0, RollType.Normal)));
             _mover.ResolveAttack(Arg.Any<OpenCombatEngine.Core.Models.Combat.AttackResult>())
                .Returns(new OpenCombatEngine.Core.Models.Combat.AttackOutcome(true, 5, "Hit"));

            // Act
            var result = moveAction.Execute(context);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _attacker.ActionEconomy.Received(1).UseReaction();
        }
    }
}
