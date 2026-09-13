using System;
using System.Linq;
using FluentAssertions;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Features;
using OpenCombatEngine.Implementation.Items;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Features
{
    public class SkillBonusFeatureTests
    {
        [Fact]
        public void OnApplied_RaisesOnlyTheNamedSkillsModifier()
        {
            // Proves the motivating example end to end: a "+2
            // Intimidation" item raises exactly that skill's real,
            // sheet-displayed modifier and leaves every other skill —
            // including Persuasion/Deception, which share Intimidation's
            // governing Charisma ability — untouched.
            var scores = new StandardAbilityScores(charisma: 10); // +0 modifier
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Hero", scores, new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            var feature = new SkillBonusFeature("Ring of Intimidation", "A cowed glare.", "Intimidation", 2);

            creature.AddFeature(feature);
            var state = creature.GetState();

            state.Skills!.Single(s => s.Name == "Intimidation").Modifier.Should().Be(2);
            state.Skills!.Single(s => s.Name == "Persuasion").Modifier.Should().Be(0);
            state.Skills!.Single(s => s.Name == "Deception").Modifier.Should().Be(0);
        }

        [Fact]
        public void OnRemoved_RevertsTheBonus()
        {
            var scores = new StandardAbilityScores(charisma: 10);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Hero", scores, new StandardHitPoints(10), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            var feature = new SkillBonusFeature("Ring of Intimidation", "A cowed glare.", "Intimidation", 2);

            creature.AddFeature(feature);
            creature.RemoveFeature(feature);
            var state = creature.GetState();

            state.Skills!.Single(s => s.Name == "Intimidation").Modifier.Should().Be(0);
        }

        [Fact]
        public void Constructor_Should_Throw_On_Empty_Name()
        {
            Action act = () => new SkillBonusFeature("", "desc", "Intimidation", 2);

            act.Should().Throw<ArgumentException>().WithMessage("*Name cannot be empty*");
        }

        [Fact]
        public void Constructor_Should_Throw_On_Empty_SkillName()
        {
            Action act = () => new SkillBonusFeature("Ring", "desc", "", 2);

            act.Should().Throw<ArgumentException>().WithMessage("*Skill name cannot be empty*");
        }
    }
}
