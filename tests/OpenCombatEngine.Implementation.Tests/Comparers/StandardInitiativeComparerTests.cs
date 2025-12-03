using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models;
using OpenCombatEngine.Implementation.Comparers;
using Xunit;
using System;
using System.Collections.Generic;

namespace OpenCombatEngine.Implementation.Tests.Comparers
{
    public class StandardInitiativeComparerTests
    {
        [Fact]
        public void Compare_Should_Rank_Higher_Total_Greater()
        {
            var comparer = new StandardInitiativeComparer();
            var c1 = Substitute.For<ICreature>();
            var c2 = Substitute.For<ICreature>();

            var roll1 = new InitiativeRoll(c1, 20, 10);
            var roll2 = new InitiativeRoll(c2, 10, 10);

            // roll1 > roll2
            comparer.Compare(roll1, roll2).Should().BePositive();
            comparer.Compare(roll2, roll1).Should().BeNegative();
        }

        [Fact]
        public void Compare_Should_Rank_Higher_Dex_Greater_If_Total_Equal()
        {
            var comparer = new StandardInitiativeComparer();
            var c1 = Substitute.For<ICreature>();
            var c2 = Substitute.For<ICreature>();

            var roll1 = new InitiativeRoll(c1, 15, 18); // Higher Dex
            var roll2 = new InitiativeRoll(c2, 15, 10); // Lower Dex

            // roll1 > roll2
            comparer.Compare(roll1, roll2).Should().BePositive();
        }

        [Fact]
        public void Compare_Should_Use_Id_TieBreaker_If_Total_And_Dex_Equal()
        {
            var comparer = new StandardInitiativeComparer();
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            
            // Ensure id1 > id2 for test
            if (id1.CompareTo(id2) < 0) (id1, id2) = (id2, id1);

            var c1 = Substitute.For<ICreature>();
            c1.Id.Returns(id1);
            var c2 = Substitute.For<ICreature>();
            c2.Id.Returns(id2);

            var roll1 = new InitiativeRoll(c1, 15, 10);
            var roll2 = new InitiativeRoll(c2, 15, 10);

            // roll1 > roll2 because id1 > id2
            comparer.Compare(roll1, roll2).Should().BePositive();
        }
    }
}
