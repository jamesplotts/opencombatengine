using System.Collections.Generic;
using FluentAssertions;
using OpenCombatEngine.Implementation.Open5e;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Open5e
{
    /// <summary>
    /// Tests for <see cref="Open5eSpellTextParser"/> — the prose-based
    /// damage/save extraction added after live testing found every
    /// Open5e-sourced spell dealt zero damage: Open5e's REST API has no
    /// structured damage/save fields for spells at all, only free-text
    /// <c>desc</c>. Descriptions here are the real text returned by
    /// api.open5e.com for each named spell, not fabricated, so a phrasing
    /// change in real SRD text would show up as a real test failure.
    /// </summary>
    public class Open5eSpellTextParserTests
    {
        [Fact]
        public void ExtractDamageRolls_MagicMissile_ReturnsSingleForceDamageWithFlatModifier()
        {
            const string desc = "You create three glowing darts of magical force. Each dart hits a creature of your choice that you can see within range. A dart deals 1d4 + 1 force damage to its target. The darts all strike simultaneously, and you can direct them to hit one creature or several.";

            var rolls = Open5eSpellTextParser.ExtractDamageRolls(desc);

            rolls.Should().ContainSingle();
            rolls[0].Dice.Should().Be("1d4+1");
            rolls[0].Type.Should().Be("FORCE");
        }

        [Fact]
        public void ExtractDamageRolls_Fireball_ReturnsSingleFireDamageRoll()
        {
            const string desc = "A bright streak flashes from your pointing finger to a point you choose within range and then blossoms with a low roar into an explosion of flame. Each creature in a 20-foot-radius sphere centered on that point must make a dexterity saving throw. A target takes 8d6 fire damage on a failed save, or half as much damage on a successful one.";

            var rolls = Open5eSpellTextParser.ExtractDamageRolls(desc);

            rolls.Should().ContainSingle();
            rolls[0].Dice.Should().Be("8d6");
            rolls[0].Type.Should().Be("FIRE");
        }

        [Fact]
        public void ExtractDamageRolls_CureWounds_ReturnsEmpty_HealingIsNotDamage()
        {
            const string desc = "A creature you touch regains a number of hit points equal to 1d8 + your spellcasting ability modifier. This spell has no effect on undead or constructs.";

            var rolls = Open5eSpellTextParser.ExtractDamageRolls(desc);

            rolls.Should().BeEmpty();
        }

        [Fact]
        public void ExtractDamageRolls_NullOrBlankText_ReturnsEmptyNotNull()
        {
            Open5eSpellTextParser.ExtractDamageRolls(null).Should().BeEmpty();
            Open5eSpellTextParser.ExtractDamageRolls("").Should().BeEmpty();
            Open5eSpellTextParser.ExtractDamageRolls("   ").Should().BeEmpty();
        }

        [Fact]
        public void ExtractDamageRolls_NoRecognizedPhrasing_ReturnsEmpty()
        {
            const string desc = "You gain the ability to see in the dark for the duration.";

            Open5eSpellTextParser.ExtractDamageRolls(desc).Should().BeEmpty();
        }

        public static IEnumerable<object[]> SingleDamagePhraseCases()
        {
            yield return new object[] { "The target takes 1d8 cold damage, and its speed is reduced by 10 feet.", "1d8", "COLD" };
            yield return new object[] { "On a hit, the target takes 3d10 necrotic damage.", "3d10", "NECROTIC" };
            yield return new object[] { "The creature must succeed on a Constitution saving throw or take 1d12 poison damage.", "1d12", "POISON" };
        }

        [Theory]
        [MemberData(nameof(SingleDamagePhraseCases))]
        public void ExtractDamageRolls_VariousRealDescriptions_ExtractsExpectedDiceAndType(string desc, string expectedDice, string expectedType)
        {
            var rolls = Open5eSpellTextParser.ExtractDamageRolls(desc);

            rolls.Should().ContainSingle();
            rolls[0].Dice.Should().Be(expectedDice);
            rolls[0].Type.Should().Be(expectedType);
        }

        [Fact]
        public void ExtractSavingThrowAbility_Fireball_ReturnsDEX()
        {
            const string desc = "Each creature in a 20-foot-radius sphere centered on that point must make a dexterity saving throw.";

            Open5eSpellTextParser.ExtractSavingThrowAbility(desc).Should().Be("DEX");
        }

        [Fact]
        public void ExtractSavingThrowAbility_PoisonSpray_ReturnsCON()
        {
            const string desc = "The creature must succeed on a Constitution saving throw or take 1d12 poison damage.";

            Open5eSpellTextParser.ExtractSavingThrowAbility(desc).Should().Be("CON");
        }

        [Fact]
        public void ExtractSavingThrowAbility_MagicMissile_ReturnsNull_NoSaveNoAttackAutoHit()
        {
            const string desc = "A dart deals 1d4 + 1 force damage to its target. The darts all strike simultaneously.";

            Open5eSpellTextParser.ExtractSavingThrowAbility(desc).Should().BeNull();
        }

        [Fact]
        public void ExtractSavingThrowAbility_NullOrBlankText_ReturnsNull()
        {
            Open5eSpellTextParser.ExtractSavingThrowAbility(null).Should().BeNull();
            Open5eSpellTextParser.ExtractSavingThrowAbility("").Should().BeNull();
        }
    }
}
