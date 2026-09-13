// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.CharacterCreation;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Results;
using OpenCombatEngine.Implementation.CharacterCreation;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Spells;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.CharacterCreation
{
    /// <summary>
    /// Tests for <see cref="StandardCharacterCreationService"/> — walks the
    /// real prompt/answer sequence end to end (detailed mode) and confirms
    /// quick mode's "race/class/gender, the rest is rolled" contract, per
    /// design doc's join-time "roll a new character" path.
    /// </summary>
    public class StandardCharacterCreationServiceTests
    {
        private static InMemorySpellRepository SeededSpellRepository()
        {
            var repo = new InMemorySpellRepository();
            var roller = new StandardDiceRoller();

            void Add(string name, int level, params string[] classes) =>
                repo.AddSpell(new Spell(name, level, SpellSchool.Evocation, "1 Action", "60 feet", "V, S", "Instantaneous", "Test spell.", roller, classes: classes));

            // Real SRD spell names, minimal fake mechanical data — enough
            // for these tests to verify class/level filtering, not real
            // damage resolution.
            Add("Fire Bolt", 0, "Wizard", "Sorcerer");
            Add("Mage Hand", 0, "Wizard", "Sorcerer", "Bard", "Warlock");
            Add("Light", 0, "Wizard", "Cleric", "Bard", "Sorcerer");
            Add("Ray of Frost", 0, "Wizard", "Sorcerer");
            Add("Magic Missile", 1, "Wizard", "Sorcerer");
            Add("Shield", 1, "Wizard", "Sorcerer");
            Add("Burning Hands", 1, "Wizard", "Sorcerer");
            Add("Guidance", 0, "Cleric", "Druid");
            Add("Sacred Flame", 0, "Cleric");
            Add("Thaumaturgy", 0, "Cleric");
            Add("Cure Wounds", 1, "Cleric", "Druid", "Bard", "Ranger", "Paladin");
            Add("Bless", 1, "Cleric", "Paladin");
            Add("Guiding Bolt", 1, "Cleric");
            return repo;
        }

        private static StandardCharacterCreationService NewService(out InMemorySpellRepository spellRepository, IDiceRoller? roller = null)
        {
            spellRepository = SeededSpellRepository();
            return new StandardCharacterCreationService(roller ?? new StandardDiceRoller(), spellRepository);
        }

        /// <summary>
        /// A fixed 4d6 sequence for the dice roller — the Nth call to
        /// Roll("4d6") returns rolls[N]'s individual dice (the last entry
        /// repeats for any call beyond what's given, matching NSubstitute's
        /// own multi-return semantics). Lets a test control exactly what
        /// StandardCharacterCreationService's 4d6-drop-lowest path rolls.
        /// </summary>
        private static IDiceRoller FixedD6Rolls(params int[][] rolls)
        {
            var roller = Substitute.For<IDiceRoller>();
            var results = rolls
                .Select(r => Result<DiceRollResult>.Success(new DiceRollResult(r.Sum(), "4d6", r.ToList(), 0, RollType.Normal)))
                .ToArray();
            roller.Roll("4d6").Returns(results[0], results.Skip(1).ToArray());
            return roller;
        }

        /// <summary>
        /// Walks race/class/gender/background/ability-method, then answers
        /// the by-ability assignment prompts in scoreAssignmentOrder —
        /// applied to the fixed Strength/Dexterity/Constitution/
        /// Intelligence/Wisdom/Charisma order NextAssignScorePrompt always
        /// asks in, never a caller-chosen ability order (that choice no
        /// longer exists; the player picks the *score*, not the ability).
        /// </summary>
        private static CharacterCreationPrompt WalkDetailedNonCaster(StandardCharacterCreationService service, string sessionId, string race, string className, string gender, string background, string abilityMethod, IReadOnlyList<int> scoreAssignmentOrder)
        {
            service.Start(sessionId, CharacterCreationMode.Detailed, "Kestrel");
            service.Answer(sessionId, race);
            service.Answer(sessionId, className);
            service.Answer(sessionId, gender);
            service.Answer(sessionId, background);
            var prompt = service.Answer(sessionId, abilityMethod);
            foreach (var score in scoreAssignmentOrder)
            {
                prompt.Done.Should().BeFalse();
                prompt = service.Answer(sessionId, score.ToString());
            }
            return prompt;
        }

        [Fact]
        public void Start_UnspecifiedMode_ReturnsFailure()
        {
            var service = NewService(out _);
            var result = service.Start("s1", CharacterCreationMode.Unspecified, "Kestrel");
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Start_ReturnsRacePromptWithAllFourRaces()
        {
            var service = NewService(out _);
            var result = service.Start("s1", CharacterCreationMode.Detailed, "Kestrel");

            result.Success.Should().BeTrue();
            result.Done.Should().BeFalse();
            result.Choices.Should().BeEquivalentTo(new[] { "Human", "Elf", "Dwarf", "Halfling" });
        }

        [Fact]
        public void Answer_UnknownSessionId_ReturnsFailure()
        {
            var service = NewService(out _);
            var result = service.Answer("does-not-exist", "Human");
            result.Success.Should().BeFalse();
        }

        [Theory]
        [InlineData("NotARealRace")]
        [InlineData("")]
        public void Answer_InvalidRace_ReturnsFailureAndDoesNotAdvance(string badAnswer)
        {
            var service = NewService(out _);
            service.Start("s1", CharacterCreationMode.Detailed, "Kestrel");

            var result = service.Answer("s1", badAnswer);

            result.Success.Should().BeFalse();
            // Session is still on the race prompt — a subsequent real
            // answer must still be accepted as a race, not skipped past.
            var retry = service.Answer("s1", "Human");
            retry.Success.Should().BeTrue();
            retry.PromptText.Should().Contain("class");
        }

        [Fact]
        public void DetailedMode_Fighter_FullSequence_ProducesCorrectCharacter()
        {
            var service = NewService(out _);
            // Assignment now proceeds in the fixed Str,Dex,Con,Int,Wis,Cha
            // order — to reproduce the original Str=15/Con=14/Dex=13 result,
            // answer 15 for Strength, 13 for Dexterity, 14 for Constitution.
            var scoreOrder = new[] { 15, 13, 14, 12, 10, 8 };

            var result = WalkDetailedNonCaster(service, "fighter-session", "Human", "Fighter", "Male", "Soldier", "standard_array", scoreOrder);

            result.Success.Should().BeTrue();
            result.Done.Should().BeTrue();
            result.Character.Should().NotBeNull();
            var character = result.Character!;

            character.Name.Should().Be("Kestrel");
            character.Gender.Should().Be("Male");
            character.RaceName.Should().Be("Human");
            character.Background.Should().Be("Soldier");
            character.Team.Should().Be("Player");
            // Human: +1 to every ability. Assignment order is fixed
            // (Str,Dex,Con,Int,Wis,Cha); scoreOrder above answers
            // 15/13/14/12/10/8 for those slots respectively.
            character.AbilityScores.Strength.Should().Be(16);
            character.AbilityScores.Constitution.Should().Be(15);
            character.AbilityScores.Dexterity.Should().Be(14);
            // Fighter hit die 10, CON 15 -> modifier +2 -> HP 12.
            character.HitPoints.Max.Should().Be(12);
            character.HitPoints.Current.Should().Be(12);
            // AC = 10 + DEX modifier. DEX 14 -> modifier +2 -> AC 12.
            character.CombatStats!.ArmorClass.Should().Be(12);
            character.Inventory!.Gold.Should().Be(10); // Soldier's starting gold
            character.Spellcasting.Should().BeNull("Fighter is not a spellcaster");
        }

        [Theory]
        [InlineData("Fighter")]
        [InlineData("Rogue")]
        public void DetailedMode_NonCasterClasses_NeverPromptForSpells(string className)
        {
            var service = NewService(out _);
            var scoreOrder = new[] { 15, 14, 13, 12, 10, 8 };
            var result = WalkDetailedNonCaster(service, "session-" + className, "Human", className, "Male", "Criminal", "standard_array", scoreOrder);

            result.Done.Should().BeTrue("a non-caster has nothing left to ask after ability scores");
            result.PromptText.Should().BeNullOrEmpty();
        }

        [Fact]
        public void AbilityMethod_StandardArray_AsksByAbilityOfferingRemainingScores()
        {
            // standard_array shares the same AssignScores phase as the
            // dice method, so it gets the same by-ability wording as a
            // natural side effect, not separate scope: "Assign which
            // score to Strength?" with the standard array's values as
            // buttons, rather than "Assign the score 15 to which ability?".
            var service = NewService(out _);
            service.Start("std-array-session", CharacterCreationMode.Detailed, "Kestrel");
            service.Answer("std-array-session", "Human");
            service.Answer("std-array-session", "Fighter");
            service.Answer("std-array-session", "Male");
            service.Answer("std-array-session", "Soldier");

            var prompt = service.Answer("std-array-session", "standard_array");

            prompt.PromptText.Should().Be("Assign which score to Strength?");
            prompt.Choices.Should().BeEquivalentTo(new[] { "15", "14", "13", "12", "10", "8" });
        }

        [Fact]
        public void AbilityMethod_Random4d6DropLowest_ReturnsSixDiceSetsWithNoChoicesYet()
        {
            // Regression test for a live player report: choosing 4d6-drop-
            // lowest used to blind-assign six totals with zero visibility
            // into what was rolled. This is the reveal step's shape —
            // Choices must be empty (nothing to answer until the player
            // has watched the dice land) and every set must carry real
            // per-die data, not a pre-collapsed total.
            var roller = FixedD6Rolls(
                new[] { 5, 3, 2, 6 },   // drop 2 -> 14
                new[] { 2, 2, 3, 5 },   // tie on the lowest -> drop one 2 -> 10
                new[] { 4, 4, 4, 4 },   // drop 4 -> 12
                new[] { 1, 1, 1, 6 },   // drop 1 -> 8
                new[] { 6, 6, 6, 6 },   // drop 6 -> 18
                new[] { 3, 3, 3, 3 });  // drop 3 -> 9
            var service = NewService(out _, roller);
            service.Start("d6-session", CharacterCreationMode.Detailed, "Kestrel");
            service.Answer("d6-session", "Human");
            service.Answer("d6-session", "Fighter");
            service.Answer("d6-session", "Male");
            service.Answer("d6-session", "Soldier");

            var prompt = service.Answer("d6-session", "random_4d6_drop_lowest");

            prompt.Success.Should().BeTrue();
            prompt.Done.Should().BeFalse();
            prompt.Choices.Should().BeEmpty("nothing is answerable until the player has watched all six sets reveal");
            prompt.AbilityScoreRolls.Should().HaveCount(6);
            var sets = prompt.AbilityScoreRolls!;
            sets.Select(s => s.Total).Should().Equal(14, 10, 12, 8, 18, 9);
            foreach (var set in sets)
            {
                set.Dice.Should().HaveCount(4);
                set.Dice.Count(d => d.Dropped).Should().Be(1, "exactly one die drops per 4d6 set, even with a tie for lowest");
                set.Dice.Where(d => !d.Dropped).Sum(d => d.Value).Should().Be(set.Total);
            }
            // The tie case: dice [2,2,3,5] drops the first 2 (index 0), not
            // the second — deterministic, and either way the total (10) is
            // identical, but this pins the exact behavior against a change.
            sets[1].Dice[0].Dropped.Should().BeTrue();
            sets[1].Dice[1].Dropped.Should().BeFalse();
        }

        [Fact]
        public void AbilityMethod_Random4d6DropLowest_AckAdvancesToByAbilityAssignmentPrompt()
        {
            var roller = FixedD6Rolls(
                new[] { 5, 3, 2, 6 }, new[] { 2, 2, 3, 5 }, new[] { 4, 4, 4, 4 },
                new[] { 1, 1, 1, 6 }, new[] { 6, 6, 6, 6 }, new[] { 3, 3, 3, 3 });
            var service = NewService(out _, roller);
            service.Start("d6-ack-session", CharacterCreationMode.Detailed, "Kestrel");
            service.Answer("d6-ack-session", "Human");
            service.Answer("d6-ack-session", "Fighter");
            service.Answer("d6-ack-session", "Male");
            service.Answer("d6-ack-session", "Soldier");
            service.Answer("d6-ack-session", "random_4d6_drop_lowest");

            // The ack's own content is never inspected — any non-empty
            // string just means "the player has seen the rolls."
            var afterAck = service.Answer("d6-ack-session", "acknowledged");

            afterAck.Success.Should().BeTrue();
            afterAck.Done.Should().BeFalse();
            afterAck.PromptText.Should().Be("Assign which score to Strength?");
            afterAck.Choices.Should().BeEquivalentTo(new[] { "14", "10", "12", "8", "18", "9" });
            afterAck.AbilityScoreRolls.Should().BeNullOrEmpty("the reveal data is only sent once, on the roll step itself");
        }

        [Fact]
        public void AbilityMethod_Random4d6DropLowest_FullWalkthrough_AssignsExactlyWhatWasRolled()
        {
            var roller = FixedD6Rolls(
                new[] { 5, 3, 2, 6 },   // 14
                new[] { 2, 2, 3, 5 },   // 10
                new[] { 4, 4, 4, 4 },   // 12
                new[] { 1, 1, 1, 6 },   // 8
                new[] { 6, 6, 6, 6 },   // 18
                new[] { 3, 3, 3, 3 });  // 9
            var service = NewService(out _, roller);
            service.Start("d6-full-session", CharacterCreationMode.Detailed, "Kestrel");
            service.Answer("d6-full-session", "Human");
            service.Answer("d6-full-session", "Fighter");
            service.Answer("d6-full-session", "Male");
            service.Answer("d6-full-session", "Soldier");
            service.Answer("d6-full-session", "random_4d6_drop_lowest");
            service.Answer("d6-full-session", "acknowledged");

            // Assign in the fixed Str,Dex,Con,Int,Wis,Cha order, picking a
            // different rolled total each time.
            var prompt = service.Answer("d6-full-session", "14"); // Strength
            prompt = service.Answer("d6-full-session", "10");     // Dexterity
            prompt = service.Answer("d6-full-session", "12");     // Constitution
            prompt = service.Answer("d6-full-session", "8");      // Intelligence
            prompt = service.Answer("d6-full-session", "18");     // Wisdom
            prompt = service.Answer("d6-full-session", "9");      // Charisma

            prompt.Done.Should().BeTrue();
            var character = prompt.Character!;
            // Human: +1 to every ability.
            character.AbilityScores.Strength.Should().Be(15);
            character.AbilityScores.Dexterity.Should().Be(11);
            character.AbilityScores.Constitution.Should().Be(13);
            character.AbilityScores.Intelligence.Should().Be(9);
            character.AbilityScores.Wisdom.Should().Be(19);
            character.AbilityScores.Charisma.Should().Be(10);
        }

        [Fact]
        public void AbilityMethod_Random4d6DropLowest_AssignScore_RejectsValueNotInRemainingPool()
        {
            var roller = FixedD6Rolls(
                new[] { 5, 3, 2, 6 }, new[] { 2, 2, 3, 5 }, new[] { 4, 4, 4, 4 },
                new[] { 1, 1, 1, 6 }, new[] { 6, 6, 6, 6 }, new[] { 3, 3, 3, 3 });
            var service = NewService(out _, roller);
            service.Start("d6-reject-session", CharacterCreationMode.Detailed, "Kestrel");
            service.Answer("d6-reject-session", "Human");
            service.Answer("d6-reject-session", "Fighter");
            service.Answer("d6-reject-session", "Male");
            service.Answer("d6-reject-session", "Soldier");
            service.Answer("d6-reject-session", "random_4d6_drop_lowest");
            service.Answer("d6-reject-session", "acknowledged");

            var rejected = service.Answer("d6-reject-session", "99");
            rejected.Success.Should().BeFalse();
            rejected.Error.Should().Contain("not one of the remaining scores");

            // A subsequent real answer is still accepted — the bad answer
            // didn't advance or corrupt the session.
            var retry = service.Answer("d6-reject-session", "14");
            retry.Success.Should().BeTrue();
            retry.PromptText.Should().Be("Assign which score to Dexterity?");
        }

        [Fact]
        public void DetailedMode_Wizard_PromptsForThreeCantripsThenLeveledSpells_UsingRealRepositorySpells()
        {
            var service = NewService(out var spellRepository);
            service.Start("wizard-session", CharacterCreationMode.Detailed, "Elowen");
            service.Answer("wizard-session", "Elf");
            service.Answer("wizard-session", "Wizard");
            service.Answer("wizard-session", "Female");
            service.Answer("wizard-session", "Acolyte");
            var prompt = service.Answer("wizard-session", "standard_array");

            // Assign scores: Intelligence gets the highest (15) so the
            // prepared-spell-count formula below is exercised at its
            // real, non-trivial value. Assignment order is fixed
            // (Str,Dex,Con,Int,Wis,Cha); Intelligence is the 4th prompt.
            foreach (var score in new[] { 8, 10, 12, 15, 13, 14 })
            {
                prompt.Done.Should().BeFalse();
                prompt = service.Answer("wizard-session", score.ToString());
            }

            // Now three cantrip prompts.
            var realCantrips = spellRepository.GetAllSpells().Where(s => s.Level == 0 && s.Classes.Contains("Wizard")).Select(s => s.Name).ToList();
            for (var i = 0; i < 3; i++)
            {
                prompt.Success.Should().BeTrue();
                prompt.Done.Should().BeFalse();
                prompt.Choices.Should().OnlyContain(c => realCantrips.Contains(c), "every offered cantrip must be real per the spell repository");
                prompt = service.Answer("wizard-session", prompt.Choices![0]);
            }

            // Intelligence 15 -> modifier +2 -> prepared = 2 + 1 (level) = 3.
            var realLeveled = spellRepository.GetAllSpells().Where(s => s.Level == 1 && s.Classes.Contains("Wizard")).Select(s => s.Name).ToList();
            var leveledChosen = 0;
            while (!prompt.Done)
            {
                prompt.Choices.Should().OnlyContain(c => realLeveled.Contains(c));
                prompt = service.Answer("wizard-session", prompt.Choices![0]);
                leveledChosen++;
            }

            leveledChosen.Should().Be(3);
            prompt.Character!.Spellcasting.Should().NotBeNull();
            prompt.Character.Spellcasting!.PreparedSpellNames.Should().HaveCount(3 + 3); // 3 cantrips + 3 leveled
            foreach (var name in prompt.Character.Spellcasting.PreparedSpellNames)
            {
                (realCantrips.Contains(name) || realLeveled.Contains(name)).Should().BeTrue($"'{name}' must be a real spell from the repository, never invented");
            }
            prompt.Character.Spellcasting.Slots.Should().ContainSingle(s => s.Level == 1 && s.Max == 2 && s.Current == 2);
            prompt.Character.Spellcasting.CastingAbility.Should().Be(Ability.Intelligence);
        }

        [Fact]
        public void DetailedMode_Cleric_MinimumOneLeveledSpell_EvenWithNegativeModifier()
        {
            var service = NewService(out var spellRepository);
            service.Start("cleric-session", CharacterCreationMode.Detailed, "Bram");
            service.Answer("cleric-session", "Dwarf");
            service.Answer("cleric-session", "Cleric");
            service.Answer("cleric-session", "Male");
            service.Answer("cleric-session", "Acolyte");
            var prompt = service.Answer("cleric-session", "standard_array");

            // Wisdom deliberately gets the lowest standard-array value (8)
            // so its modifier is negative, to prove the "minimum of one"
            // floor from the SRD formula. Assignment order is fixed
            // (Str,Dex,Con,Int,Wis,Cha); Wisdom is the 5th prompt.
            foreach (var score in new[] { 15, 14, 13, 12, 8, 10 })
                prompt = service.Answer("cleric-session", score.ToString());

            for (var i = 0; i < 3; i++)
            {
                prompt.Choices.Should().NotBeEmpty();
                prompt = service.Answer("cleric-session", prompt.Choices![0]);
            }

            var leveledChosen = 0;
            while (!prompt.Done)
            {
                prompt = service.Answer("cleric-session", prompt.Choices![0]);
                leveledChosen++;
            }

            leveledChosen.Should().Be(1, "Wisdom 8 gives a -1 modifier, but the SRD formula floors prepared spells at 1");
        }

        [Fact]
        public void QuickMode_SurfacesExactlyThreePromptsThenDone_WithValidAutoRolledContent()
        {
            var service = NewService(out var spellRepository);
            var start = service.Start("quick-1", CharacterCreationMode.Quick, "Rin");
            start.Done.Should().BeFalse();
            start.PromptText.Should().Contain("race");

            var afterRace = service.Answer("quick-1", "Elf");
            afterRace.Done.Should().BeFalse();
            afterRace.PromptText.Should().Contain("class");

            var afterClass = service.Answer("quick-1", "Wizard");
            afterClass.Done.Should().BeFalse();
            afterClass.PromptText.Should().Contain("gender");
            afterClass.Choices.Should().BeEquivalentTo(new[] { "Male", "Female" },
                "gender is a fixed choice list, never a free-text box");

            var afterGender = service.Answer("quick-1", "Female");

            afterGender.Success.Should().BeTrue();
            afterGender.Done.Should().BeTrue("quick mode has nothing left to ask after race/class/gender");
            var character = afterGender.Character!;
            character.Gender.Should().Be("Female");
            character.Background.Should().NotBeNullOrEmpty("quick mode still auto-rolls a real background, never leaves it unset");
            SrdCharacterCreationData.Backgrounds.Select(b => b.Name).Should().Contain(character.Background,
                "quick mode must never invent a background name");
            character.Spellcasting.Should().NotBeNull("Wizard is always a spellcaster, even when auto-rolled");
            var realSpellNames = spellRepository.GetAllSpells().Select(s => s.Name).ToList();
            foreach (var name in character.Spellcasting!.PreparedSpellNames)
                realSpellNames.Should().Contain(name, "quick mode must never invent a spell name");
        }

        [Fact]
        public void QuickMode_NonCasterClass_HasNoSpellcasting()
        {
            var service = NewService(out _);
            service.Start("quick-2", CharacterCreationMode.Quick, "Doran");
            service.Answer("quick-2", "Dwarf");
            service.Answer("quick-2", "Fighter");
            var result = service.Answer("quick-2", "Male");

            result.Done.Should().BeTrue();
            result.Character!.Spellcasting.Should().BeNull();
        }

        [Fact]
        public void Gender_RejectsAnyValueNotInTheChoiceList_ThenAcceptsCanonicalCasing()
        {
            var service = NewService(out _);
            service.Start("g-1", CharacterCreationMode.Quick, "Ash");
            service.Answer("g-1", "Human");
            service.Answer("g-1", "Rogue");

            var rejected = service.Answer("g-1", "Attack Helicopter");
            rejected.Success.Should().BeFalse();
            rejected.Error.Should().Contain("not a valid gender");

            // Case-insensitive, normalized to the canonical option.
            var accepted = service.Answer("g-1", "female");
            accepted.Success.Should().BeTrue();
            accepted.Character!.Gender.Should().Be("Female");
        }

        [Fact]
        public void ListClassSpells_ReturnsOnlyThatClasssRealSpells_SeparatedByLevel()
        {
            var service = NewService(out _);
            var (cantrips, leveled) = service.ListClassSpells("Cleric");

            cantrips.Should().BeEquivalentTo(new[] { "Guidance", "Light", "Sacred Flame", "Thaumaturgy" });
            leveled.Should().BeEquivalentTo(new[] { "Cure Wounds", "Bless", "Guiding Bolt" });
            cantrips.Should().NotContain("Fire Bolt", "Fire Bolt is Wizard-only, not Cleric");
        }
    }
}
