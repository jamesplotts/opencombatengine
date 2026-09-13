using FluentAssertions;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Implementation.Creatures;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Creatures
{
    public class SrdSkillsTests
    {
        [Theory]
        [InlineData("Acrobatics", Ability.Dexterity)]
        [InlineData("Animal Handling", Ability.Wisdom)]
        [InlineData("Arcana", Ability.Intelligence)]
        [InlineData("Athletics", Ability.Strength)]
        [InlineData("Deception", Ability.Charisma)]
        [InlineData("History", Ability.Intelligence)]
        [InlineData("Insight", Ability.Wisdom)]
        [InlineData("Intimidation", Ability.Charisma)]
        [InlineData("Investigation", Ability.Intelligence)]
        [InlineData("Medicine", Ability.Wisdom)]
        [InlineData("Nature", Ability.Intelligence)]
        [InlineData("Perception", Ability.Wisdom)]
        [InlineData("Performance", Ability.Charisma)]
        [InlineData("Persuasion", Ability.Charisma)]
        [InlineData("Religion", Ability.Intelligence)]
        [InlineData("Sleight of Hand", Ability.Dexterity)]
        [InlineData("Stealth", Ability.Dexterity)]
        [InlineData("Survival", Ability.Wisdom)]
        public void All_ContainsEveryRealSrdSkillWithCorrectAbility(string skillName, Ability expectedAbility)
        {
            SrdSkills.All.Should().ContainSingle(s => s.Name == skillName)
                .Which.Ability.Should().Be(expectedAbility);
        }

        [Fact]
        public void All_HasExactlyEighteenSkills()
        {
            // The real, complete SRD 5.1 skill count — this is the number
            // this project's own new Skills tab promises to always show.
            SrdSkills.All.Should().HaveCount(18);
        }

        [Theory]
        [InlineData(Ability.Strength, "STR")]
        [InlineData(Ability.Dexterity, "DEX")]
        [InlineData(Ability.Constitution, "CON")]
        [InlineData(Ability.Intelligence, "INT")]
        [InlineData(Ability.Wisdom, "WIS")]
        [InlineData(Ability.Charisma, "CHA")]
        public void Abbreviate_ReturnsTheConventionalThreeLetterForm(Ability ability, string expected)
        {
            SrdSkills.Abbreviate(ability).Should().Be(expected);
        }
    }
}
