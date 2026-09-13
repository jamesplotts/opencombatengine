using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Creatures;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Effects;
using OpenCombatEngine.Implementation.Items;
using Xunit;
using System;
using System.Linq;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class StandardCreatureTests
    {
        [Fact]
        public void Constructor_Should_Set_Properties()
        {
            // Arrange
            var scores = new StandardAbilityScores();
            var hp = new StandardHitPoints(10, 10, 0);
            var id = Guid.NewGuid();

            // Act
            var creature = new StandardCreature(id.ToString(), "Goblin", scores, hp, new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            // Assert
            creature.Name.Should().Be("Goblin");
            creature.AbilityScores.Should().BeSameAs(scores);
            creature.HitPoints.Should().BeSameAs(hp);
            creature.Id.Should().Be(id);
        }

        [Fact]
        public void Constructor_Should_Generate_Id_If_Null()
        {
            // Arrange
            var scores = new StandardAbilityScores();
            var hp = new StandardHitPoints(10, 10, 0);

            // Act
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Goblin", scores, hp, new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            // Assert
            creature.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_Name()
        {
            // Act
            Action act = () => new StandardCreature(Guid.NewGuid().ToString(), null!, new StandardAbilityScores(), new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Name cannot be empty*");
        }

        [Fact]
        public void Constructor_Should_Throw_On_Null_AbilityScores()
        {
            // Act
            Action act = () => new StandardCreature(Guid.NewGuid().ToString(), "Goblin", null!, new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetState_Abilities_ReflectsRealScoresAndModifiersInSrdOrder()
        {
            var scores = new StandardAbilityScores(strength: 15, dexterity: 12, constitution: 14, intelligence: 17, wisdom: 10, charisma: 18);
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Kestrel", scores, new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));

            var state = creature.GetState();

            state.Abilities.Should().NotBeNull();
            state.Abilities!.Select(a => (a.Name, a.Score, a.Modifier)).Should().Equal(
                ("Strength", 15, 2),
                ("Dexterity", 12, 1),
                ("Constitution", 14, 2),
                ("Intelligence", 17, 3),
                ("Wisdom", 10, 0),
                ("Charisma", 18, 4));
        }

        [Fact]
        public void GetState_Skills_ReportsAll18SrdSkills_ProficientOneHasHigherModifier()
        {
            var scores = new StandardAbilityScores(charisma: 16); // +3 modifier
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Kestrel", scores, new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            creature.Checks.AddSkillProficiency("Persuasion");

            var state = creature.GetState();

            state.Skills.Should().NotBeNull();
            state.Skills!.Should().HaveCount(18);
            var persuasion = state.Skills!.Single(s => s.Name == "Persuasion");
            persuasion.Ability.Should().Be("CHA");
            persuasion.Proficient.Should().BeTrue();
            persuasion.Modifier.Should().Be(3 + creature.ProficiencyBonus);

            var deception = state.Skills!.Single(s => s.Name == "Deception"); // also CHA, not proficient
            deception.Proficient.Should().BeFalse();
            deception.Modifier.Should().Be(3);
        }

        [Fact]
        public void GetState_Skills_ActiveAbilityCheckEffect_AppliesToEverySkillModifier()
        {
            // Proves the sheet's Skills modifier can never silently
            // disagree with what a real RollAbilityCheck actually adds —
            // both go through the exact same Effects.ApplyStatBonuses
            // hook. A whole-ability-check effect (no target skill name
            // given, e.g. a general buff like Guidance) applies uniformly
            // to every skill, proficient or not. A bonus scoped to one
            // named skill only is a different, additive effect shape —
            // see GetState_Skills_SkillScopedEffect_AppliesOnlyToThatSkill
            // below.
            var scores = new StandardAbilityScores(strength: 10); // +0 modifier
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Kestrel", scores, new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            creature.Effects.AddEffect(new StatBonusEffect("Guidance", "A minor blessing.", durationRounds: 1, StatType.AbilityCheck, bonus: 4));

            var state = creature.GetState();

            var athletics = state.Skills!.Single(s => s.Name == "Athletics"); // STR, not proficient
            athletics.Modifier.Should().Be(4); // +0 ability mod, +0 proficiency, +4 effect bonus
        }

        [Fact]
        public void GetState_Skills_SkillScopedEffect_AppliesOnlyToThatSkill()
        {
            // The literal motivating example: a "+2 Intimidation" bonus
            // (e.g. from a SkillBonusFeature-granting magic item or feat)
            // raises exactly Intimidation's modifier and leaves every
            // other skill untouched — including Deception and Persuasion,
            // which share Intimidation's governing Charisma ability, so a
            // whole-ability leak would otherwise go unnoticed.
            var scores = new StandardAbilityScores(charisma: 10); // +0 modifier
            var creature = new StandardCreature(Guid.NewGuid().ToString(), "Kestrel", scores, new StandardHitPoints(10, 10, 0), new StandardInventory(), new StandardTurnManager(new StandardDiceRoller()));
            creature.Effects.AddEffect(new StatBonusEffect("Ring of Intimidation", "A cowed glare.", durationRounds: -1, StatType.AbilityCheck, bonus: 2, targetSkillName: "Intimidation"));

            var state = creature.GetState();

            state.Skills!.Single(s => s.Name == "Intimidation").Modifier.Should().Be(2);
            state.Skills!.Single(s => s.Name == "Deception").Modifier.Should().Be(0);
            state.Skills!.Single(s => s.Name == "Persuasion").Modifier.Should().Be(0);
        }
    }
}
