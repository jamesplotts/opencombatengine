// Copyright (c) 2026 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using FluentAssertions;
using OpenCombatEngine.Implementation.Loot;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class StandardEncounterChallengeCalculatorTests
    {
        private readonly StandardEncounterChallengeCalculator _calculator = new();

        [Fact]
        public void CalculateEffectiveChallengeRating_EmptyList_ReturnsZero()
        {
            _calculator.CalculateEffectiveChallengeRating(new List<double>()).Should().Be(0d);
        }

        [Fact]
        public void CalculateEffectiveChallengeRating_SingleParticipant_ReducesToItsOwnCr()
        {
            // A one-monster "group" gets the standard x1 multiplier, so the
            // adjusted XP budget is exactly that monster's own XP value —
            // which maps straight back to its own CR.
            _calculator.CalculateEffectiveChallengeRating(new List<double> { 5d }).Should().Be(5d);
        }

        [Fact]
        public void CalculateEffectiveChallengeRating_SingleParticipant_ChallengeRatingZero_ReturnsZero()
        {
            _calculator.CalculateEffectiveChallengeRating(new List<double> { 0d }).Should().Be(0d);
        }

        [Fact]
        public void CalculateEffectiveChallengeRating_FourCr1Creatures_LandsOnDocumentedBucket()
        {
            // 4 x CR 1 (200 XP each) = 800 total XP. 4 creatures falls in
            // the "3-6 monsters" x2 multiplier bucket, so adjusted XP is
            // 1600 - the highest CR affordable at 1600 XP is CR 4 (1100
            // XP; CR 5 needs 1800).
            var result = _calculator.CalculateEffectiveChallengeRating(new List<double> { 1d, 1d, 1d, 1d });

            result.Should().Be(4d);
        }

        [Fact]
        public void CalculateEffectiveChallengeRating_FractionalCrs_ConvertCorrectly()
        {
            // 2 x CR 1/4 (50 XP each) = 100 total XP. 2 creatures gets the
            // x1.5 multiplier, so adjusted XP is 150 - the highest CR
            // affordable at 150 XP is CR 1/2 (100 XP; CR 1 needs 200).
            var result = _calculator.CalculateEffectiveChallengeRating(new List<double> { 0.25d, 0.25d });

            result.Should().Be(0.5d);
        }

        [Fact]
        public void CalculateEffectiveChallengeRating_TheUserExampleEncounter_ScalesUpWithMonsterCount()
        {
            // The user's own worked example: an evil high priest (CR 8),
            // 3 acolytes (CR 1/4 each), and 10 guards (CR 1/8 each) - 14
            // participants total, landing in the "11-14" x3 multiplier
            // bucket. The resulting effective CR should exceed the
            // priest's own CR alone (8), proving the group nets more
            // treasure than looting the priest in isolation would.
            var participants = new List<double> { 8d };
            participants.AddRange(new[] { 0.25d, 0.25d, 0.25d });
            for (var i = 0; i < 10; i++) participants.Add(0.125d);

            var soloResult = _calculator.CalculateEffectiveChallengeRating(new List<double> { 8d });
            var groupResult = _calculator.CalculateEffectiveChallengeRating(participants);

            groupResult.Should().BeGreaterThan(soloResult);
        }
    }
}
