using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Models.Spatial;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Spatial;
using OpenCombatEngine.Implementation.Reactions;
using OpenCombatEngine.Core.Interfaces.Reactions;

using OpenCombatEngine.Implementation.Items;

namespace OpenCombatEngine.Implementation.Tests.Reactions
{
    public class ReactionTests
    {
        private readonly StandardGridManager _grid;
        private readonly StandardCreature _attacker;
        private readonly StandardCreature _target;

        public ReactionTests()
        {
            _grid = new StandardGridManager();
            
            // Create Mock Creatures
            // We use simple AbilityScores/HP
            var abilities = new StandardAbilityScores(10, 10, 10, 10, 10, 10);
            var hp = new StandardHitPoints(10);
             // Removed unused armor
            
            // Attacker
            _attacker = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Attacker",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new OpenCombatEngine.Implementation.Dice.StandardDiceRoller())
            );
            _attacker.Team = "Team A";

            // Target
            _target = new StandardCreature(
                Guid.NewGuid().ToString(),
                "Target",
                new StandardAbilityScores(10, 10, 10, 10, 10, 10),
                new StandardHitPoints(10),
                new StandardInventory(),
                new StandardTurnManager(new OpenCombatEngine.Implementation.Dice.StandardDiceRoller())
            );
            _target.Team = "Team B";

            // Place on Grid
            _grid.PlaceCreature(_attacker, new Position(0, 0, 0));
            _grid.PlaceCreature(_target, new Position(1, 0, 0)); // Adjacent (5ft)
        }

        [Fact]
        public void ReactionManager_Should_Be_Initialized()
        {
            _attacker.ReactionManager.Should().NotBeNull();
            _attacker.ReactionManager.AvailableReactions.Should().NotBeEmpty(); // Should have Opportunity Attack
        }

        [Fact]
        public void OpportunityAttack_Should_Trigger_When_Leaving_Reach()
        {
            // Arrange
            // Ensure attacker has reaction
            _attacker.ActionEconomy.HasReaction.Should().BeTrue();
            
            // Move target away: From (1,0,0) [Dis 5] to (2,0,0) [Dis 10]
            // Reach is 5.
            // Moving TO 10ft means leaving 5ft reach?
            // "You can make an opportunity attack when a hostile creature that you can see moves out of your reach."
            // If reach is 5, squares within 5ft are threatened.
            // If you move FROM a threatened square TO a square > reach, you provoke.
            
            var startPos = new Position(1, 0, 0);
            var endPos = new Position(2, 0, 0); // 10ft away

            // Act
            _grid.MoveCreature(_target, endPos);

            // Assert
            // How do we verify?
            // OpportunityAttackReaction returns a successful Result<ActionResult>.
            // But currently it just returns it. Where does it go?
            // StandardReactionManager ignores the return value in the loop (except 'break').
            // We need a way to verify it happened.
            // Side effect: ActionEconomy.UseReaction().
            
            _attacker.ActionEconomy.HasReaction.Should().BeFalse("Reaction should be consumed by Opportunity Attack");
        }

        [Fact]
        public void OpportunityAttack_Should_NOT_Trigger_When_Staying_Within_Reach()
        {
            // Arrange
            _attacker.ActionEconomy.ResetTurn();
            _attacker.ActionEconomy.HasReaction.Should().BeTrue();

            // Move target around attacker: From (1,0,0) to (1,1,0) [Still 5ft]
            var startPos = new Position(1, 0, 0);
            var endPos = new Position(1, 1, 0); 

            // Act
            _grid.MoveCreature(_target, endPos);

            // Assert
            _attacker.ActionEconomy.HasReaction.Should().BeTrue("Reaction should NOT be consumed when staying in reach");
        }

        [Fact]
        public void OpportunityAttack_Should_NOT_Trigger_For_Ally()
        {
            // Arrange
            _target.Team = "Team A"; // Ally
            _attacker.ActionEconomy.ResetTurn();

            var startPos = new Position(1, 0, 0);
            var endPos = new Position(2, 0, 0); // Out of reach

            // Act
            _grid.MoveCreature(_target, endPos);

            // Assert
            _attacker.ActionEconomy.HasReaction.Should().BeTrue("Should not OA ally");
        }
    }
}
